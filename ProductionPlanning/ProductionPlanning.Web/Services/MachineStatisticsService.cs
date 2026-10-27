using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Services;

public class MachineStatisticsService
{
    public MachineStatisticsViewModel GetStatistics(int machineId, DateOnly today)
    {
        var dailyValues = Enumerable.Range(0, 7)
            .Select(daysAgo =>
            {
                var date = today.AddDays(daysAgo - 6);
                var random = new StableDemoRandom(CreateSeed(machineId, date));
                var plannedQuantity = random.Next(75, 181);
                var downtimeMinutes = random.Next(0, 121);

                return new MachineDailyStatisticsViewModel
                {
                    Date = date,
                    PlannedQuantity = plannedQuantity,
                    ProducedQuantity = random.Next(45, plannedQuantity + 31),
                    DowntimeMinutes = downtimeMinutes,
                    DowntimeIntervals = CreateDowntimeIntervals(
                        downtimeMinutes,
                        ref random)
                };
            })
            .ToArray();

        return new MachineStatisticsViewModel
        {
            DailyValues = dailyValues
        };
    }

    private static IReadOnlyList<MachineDowntimeIntervalViewModel>
        CreateDowntimeIntervals(int totalMinutes, ref StableDemoRandom random)
    {
        if (totalMinutes == 0)
        {
            return [];
        }

        var intervalCount = Math.Min(random.Next(1, 4), totalMinutes);
        var remainingMinutes = totalMinutes;
        var currentMinute = 8 * 60 + random.Next(0, 61);
        var intervals = new List<MachineDowntimeIntervalViewModel>(intervalCount);

        for (var index = 0; index < intervalCount; index++)
        {
            var intervalsLeft = intervalCount - index;
            var duration = intervalsLeft == 1
                ? remainingMinutes
                : random.Next(1, remainingMinutes - intervalsLeft + 2);

            var endMinute = currentMinute + duration;
            intervals.Add(new MachineDowntimeIntervalViewModel
            {
                StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(currentMinute)),
                EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(endMinute)),
                DurationMinutes = duration
            });

            remainingMinutes -= duration;
            currentMinute = endMinute + random.Next(15, 91);
        }

        return intervals;
    }

    private static uint CreateSeed(int machineId, DateOnly date)
    {
        var seed = unchecked(
            (uint)machineId * 2_654_435_761u ^
            (uint)date.DayNumber * 2_246_822_519u);

        return seed == 0 ? 0x9E3779B9u : seed;
    }

    private struct StableDemoRandom(uint seed)
    {
        private uint _state = seed;

        public int Next(int minimum, int maximumExclusive)
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;

            return minimum + (int)(_state % (uint)(maximumExclusive - minimum));
        }
    }
}
