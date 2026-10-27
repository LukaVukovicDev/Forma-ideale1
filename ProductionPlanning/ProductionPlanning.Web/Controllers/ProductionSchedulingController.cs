using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Services;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Controllers;

public sealed class ProductionSchedulingController(
    ApplicationDbContext dbContext,
    ProductionSchedulingService schedulingService,
    RouteCalculationService calculationService) : Controller
{
    private static readonly string[] GanttColors =
    [
        "#376da8", "#a94c59", "#3f8367", "#8057a1", "#bd762d",
        "#287e89", "#8a6943", "#5a70a5", "#a24f83", "#557c45"
    ];

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? date, CancellationToken cancellationToken)
    {
        var timeZone = MachineAvailabilityService.GetBelgradeTimeZone();
        var selectedDate = date ?? DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone).Date);
        var plans = await dbContext.PlannedProductions
            .AsNoTracking()
            .Include(plan => plan.Product)
            .ThenInclude(product => product.ProductOperations)
            .ThenInclude(step => step.OperationDefinition)
            .OrderBy(plan => plan.PlannedDate)
            .ThenBy(plan => plan.Id)
            .ToListAsync(cancellationToken);

        var scheduledKeys = (await dbContext.ScheduledProductionOperations
            .AsNoTracking()
            .Select(item => new { item.PlannedProductionId, item.ProductOperationId })
            .ToListAsync(cancellationToken))
            .Select(item => (item.PlannedProductionId, item.ProductOperationId))
            .ToHashSet();

        var unscheduled = new List<UnscheduledOperationViewModel>();
        foreach (var plan in plans)
        {
            var steps = plan.Product.ProductOperations
                .OrderBy(item => item.SequenceNumber)
                .ToList();
            foreach (var step in steps.Where(step =>
                         !scheduledKeys.Contains((plan.Id, step.Id))))
            {
                long duration;
                string? durationError = null;
                try
                {
                    duration = calculationService.EstimateDurationSeconds(
                        [(step.SetupDurationMinutes, step.ProcessingSecondsPerUnit)],
                        plan.PlannedQuantity);
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    duration = 0;
                    durationError = exception.Message;
                }
                catch (OverflowException)
                {
                    duration = 0;
                    durationError = "Trajanje operacije je preveliko.";
                }

                var previousSteps = steps
                    .Where(previous => previous.SequenceNumber < step.SequenceNumber)
                    .ToList();
                var canSchedule = previousSteps.All(previous =>
                    scheduledKeys.Contains((plan.Id, previous.Id)));
                unscheduled.Add(new UnscheduledOperationViewModel
                {
                    PlannedProductionId = plan.Id,
                    ProductOperationId = step.Id,
                    OperationDefinitionId = step.OperationDefinitionId,
                    SequenceNumber = step.SequenceNumber,
                    OperationName = step.OperationDefinition.Name,
                    ProductName = plan.Product.Naziv,
                    ProductCode = plan.Product.SifraProizvoda,
                    Quantity = plan.PlannedQuantity,
                    PlannedDate = plan.PlannedDate,
                    DurationSeconds = duration,
                    CanSchedule = canSchedule && duration > 0 && step.OperationDefinition.IsActive,
                    SchedulingBlockReason = durationError ??
                        (!step.OperationDefinition.IsActive
                            ? "Operacija nije aktivna."
                            : !canSchedule
                                ? "Prvo zakažite prethodnu operaciju."
                                : null)
                });
            }
        }

        var capabilities = await dbContext.MachineOperationCapabilities
            .AsNoTracking()
            .Where(item => item.Machine.IsActive)
            .Select(item => new
            {
                item.MachineId,
                item.Machine.SifraMasine,
                item.Machine.Naziv,
                item.OperationDefinitionId
            })
            .OrderBy(item => item.SifraMasine)
            .ToListAsync(cancellationToken);

        var machines = capabilities
            .GroupBy(item => item.MachineId)
            .Select(group => new SchedulingMachineOptionViewModel
            {
                Id = group.Key,
                Code = group.First().SifraMasine,
                Name = group.First().Naziv,
                OperationDefinitionIds = group.Select(item => item.OperationDefinitionId).Distinct().ToArray()
            })
            .ToList();

        var dayStartLocal = selectedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var nextDayStartLocal = selectedDate.AddDays(1)
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, timeZone);
        var nextDayStartUtc = TimeZoneInfo.ConvertTimeToUtc(nextDayStartLocal, timeZone);
        var scheduled = await dbContext.ScheduledProductionOperations
            .AsNoTracking()
            .Where(item => item.StartAtUtc < nextDayStartUtc && item.EndAtUtc > dayStartUtc)
            .Include(item => item.Machine)
            .Include(item => item.PlannedProduction)
            .ThenInclude(plan => plan.Product)
            .Include(item => item.ProductOperation)
            .ThenInclude(step => step.OperationDefinition)
            .OrderBy(item => item.StartAtUtc)
            .ToListAsync(cancellationToken);

        var ganttOperations = scheduled.Select(item =>
        {
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(item.StartAtUtc, timeZone);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(item.EndAtUtc, timeZone);
            var elapsedSeconds = Math.Max(1, (item.EndAtUtc - item.StartAtUtc).TotalSeconds);
            var startMinute = localStart.TimeOfDay.TotalMinutes;
            return new ScheduledOperationViewModel
            {
                Id = item.Id,
                PlannedProductionId = item.PlannedProductionId,
                ProductOperationId = item.ProductOperationId,
                MachineId = item.MachineId,
                MachineCode = item.Machine.SifraMasine,
                MachineName = item.Machine.Naziv,
                ProductCode = item.PlannedProduction.Product.SifraProizvoda,
                ProductName = item.PlannedProduction.Product.Naziv,
                OperationName = item.ProductOperation.OperationDefinition.Name,
                SequenceNumber = item.ProductOperation.SequenceNumber,
                StartAtLocal = localStart,
                EndAtLocal = localEnd,
                LeftPercent = Math.Clamp(startMinute / 1440d * 100d, 0, 100),
                WidthPercent = Math.Clamp(elapsedSeconds / 864d, 0.6d, 100d),
                Color = GanttColors[(item.PlannedProductionId - 1) % GanttColors.Length]
            };
        }).ToList();

        return View(new ProductionSchedulingViewModel
        {
            SelectedDate = selectedDate,
            UnscheduledOperations = unscheduled,
            Machines = machines,
            ScheduledOperations = ganttOperations,
            MachineRows = (await dbContext.Machines
                    .AsNoTracking()
                    .OrderBy(machine => machine.SifraMasine)
                    .Select(machine => new { machine.Id, machine.SifraMasine, machine.Naziv })
                    .ToListAsync(cancellationToken))
                .Select(machine => new SchedulingMachineRowViewModel
                {
                    MachineId = machine.Id,
                    MachineCode = machine.SifraMasine,
                    MachineName = machine.Naziv,
                    Operations = ganttOperations
                        .Where(item => item.MachineId == machine.Id)
                        .ToList()
                })
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Schedule(
        int plannedProductionId,
        int productOperationId,
        int machineId,
        DateTime localStart,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Unesite ispravnu mašinu, operaciju, datum i početno vreme.";
            return RedirectToAction(nameof(Index), new { date });
        }

        var result = await schedulingService.ScheduleAsync(
            plannedProductionId,
            productOperationId,
            machineId,
            localStart,
            cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        var selectedDate = result.Succeeded
            ? DateOnly.FromDateTime(localStart)
            : date;
        return RedirectToAction(nameof(Index), new { date = selectedDate });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        int id,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var result = await schedulingService.CancelAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { date });
    }
}
