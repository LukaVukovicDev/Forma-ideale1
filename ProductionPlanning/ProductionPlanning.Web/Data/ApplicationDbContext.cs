using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Models;

namespace ProductionPlanning.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Machine> Machines => Set<Machine>();

    public DbSet<PlannedProduction> PlannedProductions =>
        Set<PlannedProduction>();

    public DbSet<ProductionRecord> ProductionRecords =>
        Set<ProductionRecord>();

    public DbSet<OperationDefinition> OperationDefinitions =>
        Set<OperationDefinition>();

    public DbSet<ProductOperation> ProductOperations =>
        Set<ProductOperation>();

    public DbSet<MachineOperationCapability> MachineOperationCapabilities =>
        Set<MachineOperationCapability>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Proizvodi");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.SifraProizvoda)
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(p => p.SifraProizvoda)
                .IsUnique();

            entity.Property(p => p.Naziv)
                .HasMaxLength(200)
                .IsRequired();
        });

        modelBuilder.Entity<Machine>(entity =>
        {
            entity.ToTable("Masine");

            entity.HasKey(machine => machine.Id);

            entity.Property(machine => machine.SifraMasine)
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(machine => machine.SifraMasine)
                .IsUnique();

            entity.Property(machine => machine.Naziv)
                .HasMaxLength(200)
                .IsRequired();
        });

        modelBuilder.Entity<PlannedProduction>(entity =>
        {
            entity.ToTable("PlaniraniProizvodi", table =>
                table.HasCheckConstraint(
                    "CK_PlaniraniProizvodi_PlannedQuantity_Positive",
                    "\"PlannedQuantity\" > 0"));

            entity.HasKey(plan => plan.Id);

            entity.HasIndex(plan => plan.ProductId);
            entity.HasIndex(plan => plan.MachineId);
            entity.HasIndex(plan => plan.PlannedDate);

            entity.HasOne(plan => plan.Product)
                .WithMany()
                .HasForeignKey(plan => plan.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(plan => plan.Machine)
                .WithMany(machine => machine.PlannedProductions)
                .HasForeignKey(plan => plan.MachineId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductionRecord>(entity =>
        {
            entity.ToTable("Uradjeno", table =>
                table.HasCheckConstraint(
                    "CK_Uradjeno_Quantity_Positive",
                    "\"Quantity\" > 0"));

            entity.HasKey(record => record.Id);

            entity.HasIndex(record => record.PlannedProductionId);
            entity.HasIndex(record => record.RecordedAtUtc);

            entity.HasOne(record => record.PlannedProduction)
                .WithMany(plan => plan.ProductionRecords)
                .HasForeignKey(record => record.PlannedProductionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OperationDefinition>(entity =>
        {
            entity.ToTable("TehnoloskeOperacije");

            entity.HasKey(operation => operation.Id);

            entity.Property(operation => operation.Code)
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(operation => operation.Code)
                .IsUnique();

            entity.Property(operation => operation.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(operation => operation.Description)
                .HasMaxLength(1000);
        });

        modelBuilder.Entity<ProductOperation>(entity =>
        {
            entity.ToTable("OperacijeProizvoda", table =>
            {
                table.HasCheckConstraint(
                    "CK_OperacijeProizvoda_SequenceNumber_Positive",
                    "\"SequenceNumber\" > 0");
                table.HasCheckConstraint(
                    "CK_OperacijeProizvoda_SetupDurationMinutes_NonNegative",
                    "\"SetupDurationMinutes\" >= 0");
                table.HasCheckConstraint(
                    "CK_OperacijeProizvoda_ProcessingSecondsPerUnit_Positive",
                    "\"ProcessingSecondsPerUnit\" > 0");
            });

            entity.HasKey(step => step.Id);

            entity.HasIndex(step => new { step.ProductId, step.SequenceNumber })
                .IsUnique();
            entity.HasIndex(step => step.OperationDefinitionId);

            entity.HasOne(step => step.Product)
                .WithMany(product => product.ProductOperations)
                .HasForeignKey(step => step.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(step => step.OperationDefinition)
                .WithMany(operation => operation.ProductOperations)
                .HasForeignKey(step => step.OperationDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MachineOperationCapability>(entity =>
        {
            entity.ToTable("KompatibilnostMasinaIOperacija");

            entity.HasKey(capability => capability.Id);

            entity.HasIndex(capability => new
                {
                    capability.MachineId,
                    capability.OperationDefinitionId
                })
                .IsUnique();
            entity.HasIndex(capability => capability.OperationDefinitionId);

            entity.HasOne(capability => capability.Machine)
                .WithMany(machine => machine.OperationCapabilities)
                .HasForeignKey(capability => capability.MachineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(capability => capability.OperationDefinition)
                .WithMany(operation => operation.MachineCapabilities)
                .HasForeignKey(capability => capability.OperationDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}