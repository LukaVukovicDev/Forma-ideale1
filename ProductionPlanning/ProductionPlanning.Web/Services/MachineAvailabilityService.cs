using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;

namespace ProductionPlanning.Web.Services;

public sealed class MachineAvailabilityService(ApplicationDbContext dbContext)
{
    public const string BelgradeTimeZoneName = "Europe/Belgrade";

    private static readonly TimeZoneInfo BelgradeTimeZone = ResolveBelgradeTimeZone();

    public async Task<MachineAvailabilityDay> GetDailyAvailabilityAsync(
        int machineId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Machines
            .AsNoTracking()
            .AnyAsync(machine => machine.Id == machineId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException($"Mašina sa ID {machineId} ne postoji.");
        }

        var day = (DayOfWeek)date.DayOfWeek;
        var working = await dbContext.MachineWorkingIntervals
            .AsNoTracking()
            .Where(item => item.MachineId == machineId && item.DayOfWeek == day)
            .Select(item => new AvailabilityRange(item.StartTime, item.EndTime))
            .ToListAsync(cancellationToken);
        var breaks = await dbContext.MachineBreaks
            .AsNoTracking()
            .Where(item => item.MachineId == machineId && item.DayOfWeek == day)
            .Select(item => new AvailabilityRange(item.StartTime, item.EndTime))
            .ToListAsync(cancellationToken);
        var exceptions = await dbContext.MachineCalendarExceptions
            .AsNoTracking()
            .Where(item => item.MachineId == machineId && item.Date == date)
            .Select(item => new
            {
                item.StartTime,
                item.EndTime,
                item.ExceptionType
            })
            .ToListAsync(cancellationToken);

        var regular = working.Select(range => ToMinutes(range, date)).ToList();
        var extra = exceptions
            .Where(item => item.ExceptionType == MachineCalendarExceptionType.ExtraWorkingTime)
            .Select(item => ToMinutes(item.StartTime, item.EndTime, date))
            .ToList();
        var unavailable = exceptions
            .Where(item => item.ExceptionType == MachineCalendarExceptionType.Unavailable)
            .Select(item => ToMinutes(item.StartTime, item.EndTime, date))
            .ToList();
        var breakMinutes = breaks.Select(range => ToMinutes(range, date)).ToList();

        var points = new SortedSet<int> { 0, 1440 };
        AddBoundaries(points, regular);
        AddBoundaries(points, extra);
        AddBoundaries(points, unavailable);
        AddBoundaries(points, breakMinutes);

        var pointsArray = points.ToArray();
        var timeline = new List<MachineAvailabilitySegment>(pointsArray.Length - 1);
        for (var index = 0; index < pointsArray.Length - 1; index++)
        {
            var start = pointsArray[index];
            var end = pointsArray[index + 1];
            if (start == end)
            {
                continue;
            }

            var kind = unavailable.Any(range => range.Contains(start))
                ? MachineAvailabilityKind.Unavailable
                : breakMinutes.Any(range => range.Contains(start))
                    ? MachineAvailabilityKind.Break
                    : regular.Any(range => range.Contains(start))
                        ? MachineAvailabilityKind.Regular
                        : extra.Any(range => range.Contains(start))
                            ? MachineAvailabilityKind.Extra
                            : MachineAvailabilityKind.OffHours;

            if (timeline.Count > 0 &&
                timeline[^1].Kind == kind &&
                timeline[^1].EndMinute == start)
            {
                timeline[^1] = timeline[^1] with { EndMinute = end };
            }
            else
            {
                timeline.Add(new MachineAvailabilitySegment(start, end, kind));
            }
        }

        var available = timeline
            .Where(segment => segment.Kind is MachineAvailabilityKind.Regular or MachineAvailabilityKind.Extra)
            .Select(segment => new AvailabilityRange(segment.StartMinute, segment.EndMinute))
            .ToList();
        var effective = Subtract(available, breakMinutes);
        effective = Subtract(effective, unavailable);
        effective = Merge(effective);

        var minutes = effective.Sum(range =>
            GetElapsedMinutes(date, range.StartMinute, range.EndMinute));

        return new MachineAvailabilityDay(
            machineId,
            date,
            BelgradeTimeZoneName,
            minutes,
            effective.Select(range => new MachineAvailabilityInterval(
                TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(range.StartMinute)),
                TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(range.EndMinute)),
                range.StartMinute,
                range.EndMinute)).ToList(),
            timeline);
    }

    public static TimeZoneInfo GetBelgradeTimeZone() => BelgradeTimeZone;

    private static AvailabilityRange ToMinutes(AvailabilityRange range, DateOnly date) =>
        ToMinutes(range.Start, range.End, date);

    private static AvailabilityRange ToMinutes(TimeOnly start, TimeOnly end, DateOnly date)
    {
        if (start >= end)
        {
            throw new InvalidOperationException("Vreme početka mora biti pre vremena završetka.");
        }

        ValidateWallTime(date, start);
        ValidateWallTime(date, end);
        return new AvailabilityRange(
            (int)start.ToTimeSpan().TotalMinutes,
            (int)end.ToTimeSpan().TotalMinutes);
    }

    private static void ValidateWallTime(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        if (BelgradeTimeZone.IsInvalidTime(local))
        {
            throw new InvalidOperationException(
                $"Vreme {time:HH:mm} ne postoji u vremenskoj zoni Europe/Belgrade na datum {date:dd.MM.yyyy.}.");
        }

        if (BelgradeTimeZone.IsAmbiguousTime(local))
        {
            throw new InvalidOperationException(
                $"Vreme {time:HH:mm} je dvosmisleno u vremenskoj zoni Europe/Belgrade na datum {date:dd.MM.yyyy.}; izaberite drugo vreme.");
        }
    }

    private static int GetElapsedMinutes(DateOnly date, int startMinute, int endMinute)
    {
        var start = date.ToDateTime(
            TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(startMinute)),
            DateTimeKind.Unspecified);
        var end = date.ToDateTime(
            TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(endMinute)),
            DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(start, BelgradeTimeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(end, BelgradeTimeZone);
        return checked((int)(endUtc - startUtc).TotalMinutes);
    }

    private static void AddBoundaries(
        SortedSet<int> points,
        IEnumerable<AvailabilityRange> ranges)
    {
        foreach (var range in ranges)
        {
            points.Add(range.StartMinute);
            points.Add(range.EndMinute);
        }
    }

    private static List<AvailabilityRange> Subtract(
        IEnumerable<AvailabilityRange> source,
        IEnumerable<AvailabilityRange> removals)
    {
        var result = source.ToList();
        foreach (var removal in removals)
        {
            result = result.SelectMany(range =>
            {
                if (removal.EndMinute <= range.StartMinute ||
                    removal.StartMinute >= range.EndMinute)
                {
                    return [range];
                }

                var remaining = new List<AvailabilityRange>();
                if (removal.StartMinute > range.StartMinute)
                {
                    remaining.Add(new AvailabilityRange(range.StartMinute, removal.StartMinute));
                }

                if (removal.EndMinute < range.EndMinute)
                {
                    remaining.Add(new AvailabilityRange(removal.EndMinute, range.EndMinute));
                }

                return remaining;
            }).ToList();
        }

        return result;
    }

    private static List<AvailabilityRange> Merge(IEnumerable<AvailabilityRange> ranges)
    {
        var merged = new List<AvailabilityRange>();
        foreach (var range in ranges.OrderBy(item => item.StartMinute))
        {
            if (merged.Count == 0 || range.StartMinute > merged[^1].EndMinute)
            {
                merged.Add(range);
            }
            else if (range.EndMinute > merged[^1].EndMinute)
            {
                merged[^1] = merged[^1] with { EndMinute = range.EndMinute };
            }
        }

        return merged;
    }

    private static TimeZoneInfo ResolveBelgradeTimeZone()
    {
        var id = OperatingSystem.IsWindows()
            ? "Central European Standard Time"
            : BelgradeTimeZoneName;
        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    private readonly record struct AvailabilityRange(int StartMinute, int EndMinute)
    {
        public AvailabilityRange(TimeOnly start, TimeOnly end)
            : this((int)start.ToTimeSpan().TotalMinutes, (int)end.ToTimeSpan().TotalMinutes)
        {
        }

        public TimeOnly Start => TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(StartMinute));
        public TimeOnly End => TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(EndMinute));
        public bool Contains(int minute) => minute >= StartMinute && minute < EndMinute;
    }
}

public sealed record MachineAvailabilityDay(
    int MachineId,
    DateOnly Date,
    string TimeZoneId,
    int AvailableMinutes,
    IReadOnlyList<MachineAvailabilityInterval> Intervals,
    IReadOnlyList<MachineAvailabilitySegment> Timeline);

public sealed record MachineAvailabilityInterval(
    TimeOnly StartTime,
    TimeOnly EndTime,
    int StartMinute,
    int EndMinute);

public sealed record MachineAvailabilitySegment(
    int StartMinute,
    int EndMinute,
    MachineAvailabilityKind Kind)
{
    public int WallClockMinutes => EndMinute - StartMinute;
    public string Label => Kind switch
    {
        MachineAvailabilityKind.Regular => "Redovno radno vreme",
        MachineAvailabilityKind.Extra => "Dodatno radno vreme",
        MachineAvailabilityKind.Break => "Pauza",
        MachineAvailabilityKind.Unavailable => "Nedostupno",
        _ => "Van radnog vremena"
    };
}

public enum MachineAvailabilityKind
{
    OffHours,
    Regular,
    Break,
    Unavailable,
    Extra
}
