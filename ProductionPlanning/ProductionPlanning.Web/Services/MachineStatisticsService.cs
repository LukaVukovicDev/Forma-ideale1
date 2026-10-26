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

                return new MachineDailyStatisticsViewModel
                {
                    Date = date,
                    PlannedQuantity = plannedQuantity,
                    ProducedQuantity = random.Next(45, plannedQuantity + 31),
                    DowntimeMinutes = random.Next(0, 121)
                };
            })
            .ToArray();

        return new MachineStatisticsViewModel
        {
            DailyValues = dailyValues
        };
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
