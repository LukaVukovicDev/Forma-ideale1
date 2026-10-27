using System.Data;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;

namespace ProductionPlanning.Web.Services;

public sealed class ProductionSchedulingService(
    ApplicationDbContext dbContext,
    RouteCalculationService routeCalculation,
    MachineAvailabilityService availabilityService)
{
    public async Task<ScheduleOperationResult> ScheduleAsync(
        int plannedProductionId,
        int productOperationId,
        int machineId,
        DateTime localStart,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var plan = await dbContext.PlannedProductions
                .Include(item => item.Product)
                .SingleOrDefaultAsync(item => item.Id == plannedProductionId, cancellationToken);
            if (plan is null)
            {
                return await RejectAsync(transaction, "Izabrani proizvodni nalog ne postoji.", cancellationToken);
            }

            var step = await dbContext.ProductOperations
                .Include(item => item.OperationDefinition)
                .SingleOrDefaultAsync(
                    item => item.Id == productOperationId && item.ProductId == plan.ProductId,
                    cancellationToken);
            if (step is null)
            {
                return await RejectAsync(transaction, "Izabrana operacija ne pripada proizvodu iz naloga.", cancellationToken);
            }

            if (!step.OperationDefinition.IsActive)
            {
                return await RejectAsync(transaction, "Neaktivna tehnološka operacija ne može biti zakazana.", cancellationToken);
            }

            if (await dbContext.ScheduledProductionOperations.AnyAsync(
                    item => item.PlannedProductionId == plannedProductionId &&
                            item.ProductOperationId == productOperationId,
                    cancellationToken))
            {
                return await RejectAsync(transaction, "Ova operacija proizvodnog naloga već je zakazana.", cancellationToken);
            }

            var machine = await dbContext.Machines
                .SingleOrDefaultAsync(item => item.Id == machineId && item.IsActive, cancellationToken);
            if (machine is null)
            {
                return await RejectAsync(transaction, "Izabrana mašina ne postoji ili nije aktivna.", cancellationToken);
            }

            var compatible = await dbContext.MachineOperationCapabilities
                .AnyAsync(
                    item => item.MachineId == machineId &&
                            item.OperationDefinitionId == step.OperationDefinitionId,
                    cancellationToken);
            if (!compatible)
            {
                return await RejectAsync(
                    transaction,
                    "Izabrana mašina nije kompatibilna sa ovom tehnološkom operacijom.",
                    cancellationToken);
            }

            var localStartUnspecified = DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified);
            if (localStartUnspecified < plan.PlannedDate.ToDateTime(TimeOnly.MinValue))
            {
                return await RejectAsync(
                    transaction,
                    "Operacija ne može početi pre planiranog datuma proizvodnog naloga.",
                    cancellationToken);
            }

            long durationSeconds;
            try
            {
                durationSeconds = routeCalculation.EstimateDurationSeconds(
                    [(step.SetupDurationMinutes, step.ProcessingSecondsPerUnit)],
                    plan.PlannedQuantity);
            }
            catch (ArgumentOutOfRangeException)
            {
                return await RejectAsync(
                    transaction,
                    "Trajanje operacije nije ispravno definisano u tehnološkoj ruti.",
                    cancellationToken);
            }
            catch (OverflowException)
            {
                return await RejectAsync(transaction, "Izračunato trajanje operacije je preveliko.", cancellationToken);
            }

            if (durationSeconds <= 0)
            {
                return await RejectAsync(transaction, "Trajanje operacije mora biti pozitivno.", cancellationToken);
            }

            var previousSteps = await dbContext.ProductOperations
                .Where(item => item.ProductId == plan.ProductId &&
                               item.SequenceNumber < step.SequenceNumber)
                .OrderBy(item => item.SequenceNumber)
                .Select(item => new { item.Id, item.SequenceNumber })
                .ToListAsync(cancellationToken);
            var previousSchedules = await dbContext.ScheduledProductionOperations
                .Where(item => item.PlannedProductionId == plannedProductionId &&
                               previousSteps.Select(previous => previous.Id).Contains(item.ProductOperationId))
                .ToListAsync(cancellationToken);

            if (previousSchedules.Count != previousSteps.Count)
            {
                return await RejectAsync(
                    transaction,
                    "Prvo zakažite sve prethodne operacije ovog proizvodnog naloga.",
                    cancellationToken);
            }

            var localTimeZone = MachineAvailabilityService.GetBelgradeTimeZone();
            if (localTimeZone.IsInvalidTime(localStartUnspecified) ||
                localTimeZone.IsAmbiguousTime(localStartUnspecified))
            {
                return await RejectAsync(
                    transaction,
                    "Izabrano lokalno vreme ne postoji ili je dvosmisleno zbog promene letnjeg/zimskog računanja vremena.",
                    cancellationToken);
            }

            var startAtUtc = TimeZoneInfo.ConvertTimeToUtc(localStartUnspecified, localTimeZone);
            if (previousSchedules.Any(item => item.EndAtUtc > startAtUtc))
            {
                return await RejectAsync(
                    transaction,
                    "Operacija ne može početi pre završetka prethodne operacije istog naloga.",
                    cancellationToken);
            }

            DateTime endAtUtc;
            try
            {
                endAtUtc = startAtUtc.AddSeconds(durationSeconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                return await RejectAsync(transaction, "Izračunati kraj operacije nije validan.", cancellationToken);
            }

            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endAtUtc, localTimeZone);
            if (DateOnly.FromDateTime(localEnd) != DateOnly.FromDateTime(localStartUnspecified))
            {
                return await RejectAsync(
                    transaction,
                    "Operacija mora stati u radni interval istog dana; izaberite raniji početak.",
                    cancellationToken);
            }

            if (localTimeZone.IsInvalidTime(localEnd) || localTimeZone.IsAmbiguousTime(localEnd))
            {
                return await RejectAsync(
                    transaction,
                    "Izračunati kraj operacije pada u nepostojeće ili dvosmisleno lokalno vreme.",
                    cancellationToken);
            }

            MachineAvailabilityDay availability;
            try
            {
                availability = await availabilityService.GetDailyAvailabilityAsync(
                    machineId,
                    DateOnly.FromDateTime(localStartUnspecified),
                    cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                return await RejectAsync(
                    transaction,
                    $"Kalendar mašine nije moguće proveriti: {exception.Message}",
                    cancellationToken);
            }
            var fitsAvailability = availability.Intervals.Any(interval =>
            {
                var intervalStart = DateOnly.FromDateTime(localStartUnspecified)
                    .ToDateTime(interval.StartTime, DateTimeKind.Unspecified);
                var intervalEnd = DateOnly.FromDateTime(localStartUnspecified)
                    .ToDateTime(interval.EndTime, DateTimeKind.Unspecified);
                return localStartUnspecified >= intervalStart &&
                       localEnd <= intervalEnd &&
                       (endAtUtc - startAtUtc).TotalSeconds <=
                       (TimeZoneInfo.ConvertTimeToUtc(intervalEnd, localTimeZone) -
                        TimeZoneInfo.ConvertTimeToUtc(intervalStart, localTimeZone)).TotalSeconds;
            });
            if (!fitsAvailability)
            {
                return await RejectAsync(
                    transaction,
                    "Celokupno trajanje operacije ne staje u raspoloživi radni interval mašine.",
                    cancellationToken);
            }

            var machineConflict = await dbContext.ScheduledProductionOperations
                .AnyAsync(
                    item => item.MachineId == machineId &&
                            startAtUtc < item.EndAtUtc &&
                            endAtUtc > item.StartAtUtc,
                    cancellationToken);
            if (machineConflict)
            {
                return await RejectAsync(
                    transaction,
                    "Mašina je već zauzeta u izabranom vremenskom intervalu.",
                    cancellationToken);
            }

            dbContext.ScheduledProductionOperations.Add(new ScheduledProductionOperation
            {
                PlannedProductionId = plannedProductionId,
                ProductOperationId = productOperationId,
                MachineId = machineId,
                StartAtUtc = DateTime.SpecifyKind(startAtUtc, DateTimeKind.Utc),
                EndAtUtc = DateTime.SpecifyKind(endAtUtc, DateTimeKind.Utc)
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ScheduleOperationResult.Success(
                $"Operacija „{step.OperationDefinition.Name}“ zakazana je na mašini {machine.SifraMasine}.",
                durationSeconds);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is Microsoft.Data.Sqlite.SqliteException
                  { SqliteErrorCode: 19 })
        {
            await transaction.RollbackAsync(cancellationToken);
            return ScheduleOperationResult.Failure(
                "Termin nije sačuvan zbog konflikta sa drugim zakazivanjem. Osvežite raspored i pokušajte ponovo.");
        }
        catch (Microsoft.Data.Sqlite.SqliteException exception)
            when (exception.SqliteErrorCode is 5 or 6)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ScheduleOperationResult.Failure(
                "Raspored je istovremeno izmenjen. Osvežite stranicu i pokušajte ponovo.");
        }
    }

    public async Task<ScheduleOperationResult> CancelAsync(
        int scheduledOperationId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var scheduled = await dbContext.ScheduledProductionOperations
            .Include(item => item.ProductOperation)
            .ThenInclude(item => item.OperationDefinition)
            .SingleOrDefaultAsync(item => item.Id == scheduledOperationId, cancellationToken);
        if (scheduled is null)
        {
            return await RejectAsync(transaction, "Termin više ne postoji.", cancellationToken);
        }

        var hasDependent = await dbContext.ScheduledProductionOperations
            .AnyAsync(
                item => item.PlannedProductionId == scheduled.PlannedProductionId &&
                        item.ProductOperation.SequenceNumber >
                        scheduled.ProductOperation.SequenceNumber,
                cancellationToken);
        if (hasDependent)
        {
            return await RejectAsync(
                transaction,
                "Termin nije moguće otkazati jer su zakazane kasnije operacije istog naloga. Prvo otkažite kasnije operacije.",
                cancellationToken);
        }

        dbContext.ScheduledProductionOperations.Remove(scheduled);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ScheduleOperationResult.Success(
            $"Termin operacije „{scheduled.ProductOperation.OperationDefinition.Name}“ je otkazan.",
            0);
    }

    private static async Task<ScheduleOperationResult> RejectAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        string message,
        CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        return ScheduleOperationResult.Failure(message);
    }
}

public sealed record ScheduleOperationResult(
    bool Succeeded,
    string Message,
    long DurationSeconds)
{
    public static ScheduleOperationResult Success(string message, long durationSeconds) =>
        new(true, message, durationSeconds);

    public static ScheduleOperationResult Failure(string message) =>
        new(false, message, 0);
}
