using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.Services;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Controllers;

public sealed class MachineCalendarController(
    ApplicationDbContext dbContext,
    MachineAvailabilityService availabilityService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int machineId,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        var machine = await dbContext.Machines
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == machineId, cancellationToken);
        if (machine is null)
        {
            return NotFound();
        }

        var selectedDate = date ?? DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTime.UtcNow, MachineAvailabilityService.GetBelgradeTimeZone()).Date);
        var model = new MachineCalendarViewModel
        {
            MachineId = machine.Id,
            MachineName = machine.Naziv,
            MachineCode = machine.SifraMasine,
            SelectedDate = selectedDate,
            ExceptionDate = selectedDate,
            WorkingDay = selectedDate.DayOfWeek,
            BreakDay = selectedDate.DayOfWeek,
            WorkingIntervals = await dbContext.MachineWorkingIntervals
                .AsNoTracking()
                .Where(item => item.MachineId == machineId)
                .OrderBy(item => item.DayOfWeek)
                .ThenBy(item => item.StartTime)
                .ToListAsync(cancellationToken),
            Breaks = await dbContext.MachineBreaks
                .AsNoTracking()
                .Where(item => item.MachineId == machineId)
                .OrderBy(item => item.DayOfWeek)
                .ThenBy(item => item.StartTime)
                .ToListAsync(cancellationToken),
            Exceptions = await dbContext.MachineCalendarExceptions
                .AsNoTracking()
                .Where(item => item.MachineId == machineId)
                .OrderBy(item => item.Date)
                .ThenBy(item => item.StartTime)
                .ToListAsync(cancellationToken)
        };

        try
        {
            var availability = await availabilityService.GetDailyAvailabilityAsync(
                machineId,
                selectedDate,
                cancellationToken);
            model.AvailableMinutes = availability.AvailableMinutes;
            model.TimeZoneId = availability.TimeZoneId;
            model.EffectiveIntervals = availability.Intervals;
            model.Timeline = availability.Timeline;
        }
        catch (InvalidOperationException exception)
        {
            model.AvailabilityError = exception.Message;
            model.TimeZoneId = MachineAvailabilityService.BelgradeTimeZoneName;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWorkingInterval(
        MachineCalendarViewModel model,
        CancellationToken cancellationToken)
    {
        if (!await MachineExistsAsync(model.MachineId, cancellationToken))
        {
            return NotFound();
        }

        if (!ModelState.IsValid ||
            !Enum.IsDefined(model.WorkingDay) ||
            !IsValidRange(model.WorkingStart, model.WorkingEnd))
        {
            return CalendarError(model.MachineId, model.SelectedDate,
                "Radni interval mora imati početak pre završetka i biti unutar istog dana.");
        }

        var existing = await dbContext.MachineWorkingIntervals
            .Where(item => item.MachineId == model.MachineId &&
                           item.DayOfWeek == model.WorkingDay)
            .ToListAsync(cancellationToken);
        if (existing.Any(item => Overlaps(
                model.WorkingStart, model.WorkingEnd, item.StartTime, item.EndTime)))
        {
            return CalendarError(model.MachineId, model.SelectedDate,
                "Radni intervali za isti dan ne smeju da se preklapaju.");
        }

        var existingBreaks = await dbContext.MachineBreaks
            .Where(item => item.MachineId == model.MachineId &&
                           item.DayOfWeek == model.WorkingDay)
            .ToListAsync(cancellationToken);
        var proposedIntervals = existing
            .Select(item => (item.StartTime, item.EndTime))
            .Append((model.WorkingStart, model.WorkingEnd))
            .ToList();
        if (existingBreaks.Any(item =>
                !IsContainedByUnion(item.StartTime, item.EndTime, proposedIntervals)))
        {
            return CalendarError(model.MachineId, model.SelectedDate,
                "Promena radnog vremena bi ostavila postojeću pauzu izvan radnih intervala.");
        }

        dbContext.MachineWorkingIntervals.Add(new MachineWorkingInterval
        {
            MachineId = model.MachineId,
            DayOfWeek = model.WorkingDay,
            StartTime = model.WorkingStart,
            EndTime = model.WorkingEnd
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { machineId = model.MachineId, date = model.SelectedDate });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBreak(
        MachineCalendarViewModel model,
        CancellationToken cancellationToken)
    {
        if (!await MachineExistsAsync(model.MachineId, cancellationToken))
        {
            return NotFound();
        }

        if (!ModelState.IsValid ||
            !Enum.IsDefined(model.BreakDay) ||
            !IsValidRange(model.BreakStart, model.BreakEnd) ||
            model.BreakDescription?.Length > 200)
        {
            return CalendarError(model.MachineId, model.SelectedDate,
                "Pauza mora imati početak pre završetka i biti unutar istog dana.");
        }

        var intervals = await dbContext.MachineWorkingIntervals
            .Where(item => item.MachineId == model.MachineId &&
                           item.DayOfWeek == model.BreakDay)
            .Select(item => new { item.StartTime, item.EndTime })
            .ToListAsync(cancellationToken);
        if (!IsContainedByUnion(
                model.BreakStart,
                model.BreakEnd,
                intervals.Select(item => (item.StartTime, item.EndTime)).ToList()))
        {
            return CalendarError(model.MachineId, model.SelectedDate,
                "Pauza mora biti u potpunosti unutar radnog vremena tog dana.");
        }

        var existingBreaks = await dbContext.MachineBreaks
            .Where(item => item.MachineId == model.MachineId &&
                           item.DayOfWeek == model.BreakDay)
            .ToListAsync(cancellationToken);
        if (existingBreaks.Any(item => Overlaps(
                model.BreakStart, model.BreakEnd, item.StartTime, item.EndTime)))
        {
            return CalendarError(model.MachineId, model.SelectedDate,
                "Pauze za isti dan ne smeju da se preklapaju.");
        }

        dbContext.MachineBreaks.Add(new MachineBreak
        {
            MachineId = model.MachineId,
            DayOfWeek = model.BreakDay,
            StartTime = model.BreakStart,
            EndTime = model.BreakEnd,
            Description = string.IsNullOrWhiteSpace(model.BreakDescription)
                ? null
                : model.BreakDescription.Trim()
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { machineId = model.MachineId, date = model.SelectedDate });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddException(
        MachineCalendarViewModel model,
        CancellationToken cancellationToken)
    {
        if (!await MachineExistsAsync(model.MachineId, cancellationToken))
        {
            return NotFound();
        }

        if (!ModelState.IsValid ||
            !IsValidRange(model.ExceptionStart, model.ExceptionEnd) ||
            model.ExceptionReason?.Length > 500 ||
            !Enum.IsDefined(model.ExceptionType))
        {
            return CalendarError(model.MachineId, model.SelectedDate,
                "Izuzetak mora imati ispravan tip i vremenski interval unutar istog dana.");
        }

        var existing = await dbContext.MachineCalendarExceptions
            .Where(item => item.MachineId == model.MachineId &&
                           item.Date == model.ExceptionDate &&
                           item.ExceptionType == model.ExceptionType)
            .ToListAsync(cancellationToken);
        if (existing.Any(item => Overlaps(
                model.ExceptionStart, model.ExceptionEnd, item.StartTime, item.EndTime)))
        {
            return CalendarError(model.MachineId, model.ExceptionDate,
                "Izuzeci istog tipa za isti datum ne smeju da se preklapaju.");
        }

        dbContext.MachineCalendarExceptions.Add(new MachineCalendarException
        {
            MachineId = model.MachineId,
            Date = model.ExceptionDate,
            StartTime = model.ExceptionStart,
            EndTime = model.ExceptionEnd,
            ExceptionType = model.ExceptionType,
            Reason = string.IsNullOrWhiteSpace(model.ExceptionReason)
                ? null
                : model.ExceptionReason.Trim()
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { machineId = model.MachineId, date = model.ExceptionDate });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditWorkingInterval(
        int machineId,
        int id,
        DayOfWeek day,
        TimeOnly start,
        TimeOnly end,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var interval = await dbContext.MachineWorkingIntervals
            .SingleOrDefaultAsync(item => item.Id == id && item.MachineId == machineId, cancellationToken);
        if (interval is null)
        {
            return NotFound();
        }

        if (!Enum.IsDefined(day) || !IsValidRange(start, end))
        {
            return CalendarError(machineId, date, "Radni interval nije ispravan.");
        }

        if (!ModelState.IsValid)
        {
            return CalendarError(machineId, date, "Podaci radnog intervala nisu ispravni.");
        }

        var others = await dbContext.MachineWorkingIntervals
            .Where(item => item.MachineId == machineId &&
                           item.DayOfWeek == day &&
                           item.Id != id)
            .ToListAsync(cancellationToken);
        if (others.Any(item => Overlaps(start, end, item.StartTime, item.EndTime)))
        {
            return CalendarError(machineId, date, "Radni intervali za isti dan ne smeju da se preklapaju.");
        }

        var breaks = await dbContext.MachineBreaks
            .Where(item => item.MachineId == machineId && item.DayOfWeek == day)
            .ToListAsync(cancellationToken);
        var proposed = others.Select(item => (item.StartTime, item.EndTime))
            .Append((start, end))
            .ToList();
        if (breaks.Any(item => !IsContainedByUnion(item.StartTime, item.EndTime, proposed)))
        {
            return CalendarError(machineId, date, "Izmena bi ostavila postojeću pauzu izvan radnih intervala.");
        }

        if (interval.DayOfWeek != day)
        {
            var oldDayBreaks = await dbContext.MachineBreaks
                .Where(item => item.MachineId == machineId && item.DayOfWeek == interval.DayOfWeek)
                .ToListAsync(cancellationToken);
            var oldDayIntervals = await dbContext.MachineWorkingIntervals
                .Where(item => item.MachineId == machineId &&
                               item.DayOfWeek == interval.DayOfWeek &&
                               item.Id != id)
                .Select(item => new { item.StartTime, item.EndTime })
                .ToListAsync(cancellationToken);
            if (oldDayBreaks.Any(item => !IsContainedByUnion(
                    item.StartTime,
                    item.EndTime,
                    oldDayIntervals.Select(entry => (entry.StartTime, entry.EndTime)).ToList())))
            {
                return CalendarError(machineId, date, "Izmena bi ostavila pauzu u prethodnom danu izvan radnih intervala.");
            }
        }

        interval.DayOfWeek = day;
        interval.StartTime = start;
        interval.EndTime = end;
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { machineId, date });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBreak(
        int machineId,
        int id,
        DayOfWeek day,
        TimeOnly start,
        TimeOnly end,
        string? description,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.MachineBreaks
            .SingleOrDefaultAsync(entry => entry.Id == id && entry.MachineId == machineId, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid ||
            !Enum.IsDefined(day) || !IsValidRange(start, end) || description?.Length > 200)
        {
            return CalendarError(machineId, date, "Pauza nije ispravna ili je opis predugačak.");
        }

        var intervals = await dbContext.MachineWorkingIntervals
            .Where(entry => entry.MachineId == machineId && entry.DayOfWeek == day)
            .Select(entry => new { entry.StartTime, entry.EndTime })
            .ToListAsync(cancellationToken);
        if (!IsContainedByUnion(
                start,
                end,
                intervals.Select(entry => (entry.StartTime, entry.EndTime)).ToList()))
        {
            return CalendarError(machineId, date, "Pauza mora biti u potpunosti unutar radnog vremena tog dana.");
        }

        var others = await dbContext.MachineBreaks
            .Where(entry => entry.MachineId == machineId && entry.DayOfWeek == day && entry.Id != id)
            .ToListAsync(cancellationToken);
        if (others.Any(entry => Overlaps(start, end, entry.StartTime, entry.EndTime)))
        {
            return CalendarError(machineId, date, "Pauze za isti dan ne smeju da se preklapaju.");
        }

        item.DayOfWeek = day;
        item.StartTime = start;
        item.EndTime = end;
        item.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { machineId, date });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditException(
        int machineId,
        int id,
        DateOnly exceptionDate,
        TimeOnly start,
        TimeOnly end,
        MachineCalendarExceptionType type,
        string? reason,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.MachineCalendarExceptions
            .SingleOrDefaultAsync(entry => entry.Id == id && entry.MachineId == machineId, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid ||
            !Enum.IsDefined(type) || !IsValidRange(start, end) || reason?.Length > 500)
        {
            return CalendarError(machineId, date, "Izuzetak nije ispravan ili je razlog predugačak.");
        }

        var others = await dbContext.MachineCalendarExceptions
            .Where(entry => entry.MachineId == machineId &&
                            entry.Date == exceptionDate &&
                            entry.ExceptionType == type &&
                            entry.Id != id)
            .ToListAsync(cancellationToken);
        if (others.Any(entry => Overlaps(start, end, entry.StartTime, entry.EndTime)))
        {
            return CalendarError(machineId, date, "Izuzeci istog tipa za isti datum ne smeju da se preklapaju.");
        }

        item.Date = exceptionDate;
        item.StartTime = start;
        item.EndTime = end;
        item.ExceptionType = type;
        item.Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { machineId, date = exceptionDate });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteWorkingInterval(
        int machineId,
        int id,
        DateOnly date,
        CancellationToken cancellationToken) =>
        DeleteOwnedAsync(
            dbContext.MachineWorkingIntervals,
            machineId,
            id,
            date,
            cancellationToken,
            async item =>
            {
                var breaks = await dbContext.MachineBreaks
                    .Where(entry => entry.MachineId == machineId && entry.DayOfWeek == item.DayOfWeek)
                    .ToListAsync(cancellationToken);
                var intervals = await dbContext.MachineWorkingIntervals
                    .Where(entry => entry.MachineId == machineId &&
                                    entry.DayOfWeek == item.DayOfWeek &&
                                    entry.Id != id)
                    .Select(entry => new { entry.StartTime, entry.EndTime })
                    .ToListAsync(cancellationToken);
                return breaks.All(entry => IsContainedByUnion(
                    entry.StartTime,
                    entry.EndTime,
                    intervals.Select(interval => (interval.StartTime, interval.EndTime)).ToList()));
            });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteBreak(
        int machineId,
        int id,
        DateOnly date,
        CancellationToken cancellationToken) =>
        DeleteOwnedAsync(dbContext.MachineBreaks, machineId, id, date, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> DeleteException(
        int machineId,
        int id,
        DateOnly date,
        CancellationToken cancellationToken) =>
        DeleteOwnedAsync(dbContext.MachineCalendarExceptions, machineId, id, date, cancellationToken);

    private async Task<IActionResult> DeleteOwnedAsync<TEntity>(
        DbSet<TEntity> set,
        int machineId,
        int id,
        DateOnly date,
        CancellationToken cancellationToken,
        Func<TEntity, Task<bool>>? validate = null)
        where TEntity : class
    {
        var entity = await set.SingleOrDefaultAsync(
            item => EF.Property<int>(item, "Id") == id &&
                    EF.Property<int>(item, "MachineId") == machineId,
            cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (validate is not null && !await validate(entity))
        {
            return CalendarError(machineId, date,
                "Radni interval nije moguće ukloniti dok se pauze ne premeste ili uklone.");
        }

        set.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index), new { machineId, date });
    }

    private async Task<bool> MachineExistsAsync(int machineId, CancellationToken cancellationToken) =>
        await dbContext.Machines.AnyAsync(item => item.Id == machineId, cancellationToken);

    private IActionResult CalendarError(int machineId, DateOnly date, string message)
    {
        TempData["ErrorMessage"] = message;
        return RedirectToAction(nameof(Index), new { machineId, date });
    }

    private static bool IsValidRange(TimeOnly start, TimeOnly end) =>
        start < end && start.Ticks % TimeSpan.TicksPerMinute == 0 &&
        end.Ticks % TimeSpan.TicksPerMinute == 0;

    private static bool Overlaps(
        TimeOnly start,
        TimeOnly end,
        TimeOnly otherStart,
        TimeOnly otherEnd) =>
        start < otherEnd && otherStart < end;

    private static bool IsContainedByUnion(
        TimeOnly start,
        TimeOnly end,
        IReadOnlyCollection<(TimeOnly StartTime, TimeOnly EndTime)> ranges)
    {
        var ordered = ranges.OrderBy(range => range.StartTime).ToList();
        var cursor = start;
        foreach (var range in ordered)
        {
            if (range.EndTime <= cursor)
            {
                continue;
            }

            if (range.StartTime > cursor)
            {
                return false;
            }

            cursor = range.EndTime;
            if (cursor >= end)
            {
                return true;
            }
        }

        return false;
    }
}
