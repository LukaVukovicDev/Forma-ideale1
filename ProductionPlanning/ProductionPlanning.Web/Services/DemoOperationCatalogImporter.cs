using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;

namespace ProductionPlanning.Web.Services;

public sealed class DemoOperationCatalogImporter
{
    private static readonly (string Code, string Name, string Description)[] Operations =
    [
        ("SEC", "Sečenje ploča", "Krojenje pločastog materijala prema dimenzijama elemenata proizvoda."),
        ("KAN", "Kantovanje ivica", "Nanošenje kant trake na obrađene ivice pločastih elemenata."),
        ("BUS", "Bušenje otvora", "Izrada otvora za spojnice, šarke, tiplove i druge okove."),
        ("GLO", "CNC glodanje", "Glodanje žlebova, profila i drugih oblika na elementima nameštaja."),
        ("BRU", "Brušenje", "Obrada površina radi postizanja potrebne glatkoće."),
        ("FOL", "Oblaganje MDF folijom", "Oblaganje pripremljenih MDF elemenata dekorativnom folijom."),
        ("LEP", "Lepljenje elemenata", "Nanošenje lepka i spajanje odgovarajućih delova proizvoda."),
        ("MON", "Montaža i predmontaža", "Sastavljanje delova proizvoda ili prethodna ugradnja okova."),
        ("KON", "Kontrola kvaliteta", "Provera dimenzija, kvaliteta obrade i ispravnosti elemenata."),
        ("PAK", "Pakovanje", "Priprema, zaštita i pakovanje gotovih elemenata nameštaja.")
    ];

    public async Task RunAsync(
        IServiceProvider services,
        string contentRootPath,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var databasePath = GetExistingDatabasePath(contentRootPath, connectionString);
        Console.WriteLine($"Razvojna SQLite baza: {databasePath}");

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.OpenConnectionAsync(cancellationToken);

        await VerifyExistingSchemaAsync(dbContext, cancellationToken);
        var backupPath = CreateBackup(dbContext, databasePath);
        Console.WriteLine($"Rezervna kopija: {backupPath}");

        var added = 0;
        var skipped = 0;
        await using (var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            var existingCodes = (await dbContext.OperationDefinitions
                    .AsNoTracking()
                    .Select(operation => operation.Code)
                    .ToListAsync(cancellationToken))
                .Select(NormalizeCode)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var (code, name, description) in Operations)
            {
                if (!existingCodes.Add(code))
                {
                    skipped++;
                    continue;
                }

                dbContext.OperationDefinitions.Add(new OperationDefinition
                {
                    Code = code,
                    Name = name,
                    Description = description,
                    IsActive = true
                });
                added++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        Console.WriteLine($"Uspešno dodato: {added}");
        Console.WriteLine($"Preskočeno — već postoji: {skipped}");
        Console.WriteLine("Greške: 0");
        Console.WriteLine($"Ukupno traženih operacija: {Operations.Length}");
    }

    private static string GetExistingDatabasePath(
        string contentRootPath,
        string connectionString)
    {
        var connection = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(connection.DataSource)
            || connection.DataSource == ":memory:")
        {
            throw new InvalidOperationException(
                "Import zahteva postojeću SQLite bazu na disku.");
        }

        var databasePath = Path.GetFullPath(
            connection.DataSource,
            Path.GetFullPath(contentRootPath));
        if (!File.Exists(databasePath))
        {
            throw new FileNotFoundException(
                "SQLite baza nije pronađena. Import je zaustavljen bez kreiranja nove baze.",
                databasePath);
        }

        return databasePath;
    }

    private static async Task VerifyExistingSchemaAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table' AND name = 'TehnoloskeOperacije';
            """;
        var tableExists = Convert.ToInt32(
            await tableCommand.ExecuteScalarAsync(cancellationToken)) == 1;
        if (!tableExists)
        {
            throw new InvalidOperationException(
                "Tabela TehnoloskeOperacije ne postoji. Najpre primenite fazu 1.");
        }

        await using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'index'
              AND name = 'IX_TehnoloskeOperacije_Code'
              AND tbl_name = 'TehnoloskeOperacije'
              AND sql LIKE 'CREATE UNIQUE INDEX%';
            """;
        var uniqueIndexExists = Convert.ToInt32(
            await indexCommand.ExecuteScalarAsync(cancellationToken)) == 1;
        if (!uniqueIndexExists)
        {
            throw new InvalidOperationException(
                "Očekivani jedinstveni indeks nad šifrom operacije nije pronađen.");
        }
    }

    private static string CreateBackup(
        ApplicationDbContext dbContext,
        string databasePath)
    {
        var backupPath =
            $"{databasePath}.pre-operation-import-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.bak";
        var source = (SqliteConnection)dbContext.Database.GetDbConnection();
        var destination = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = backupPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString());

        try
        {
            destination.Open();
            source.BackupDatabase(destination);
            return backupPath;
        }
        finally
        {
            destination.Dispose();
        }
    }

    private static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();
}
