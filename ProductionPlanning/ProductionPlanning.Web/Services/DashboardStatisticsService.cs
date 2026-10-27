using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Services;

public class DashboardStatisticsService
{
    public DashboardProductionStatisticsViewModel GetStatistics(DateOnly today)
    {
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var dataStart = weekStart < monthStart ? weekStart : monthStart;
        var dailyValues = Enumerable.Range(0, today.DayNumber - dataStart.DayNumber + 1)
            .Select(offset => CreateDailyValue(dataStart.AddDays(offset)))
            .ToArray();

        var monthlyValues = dailyValues
            .Where(day => day.Date >= monthStart);
        var weeklyValues = dailyValues
            .Where(day => day.Date >= weekStart);

        return new DashboardProductionStatisticsViewModel
        {
            Today = today,
            PlannedThisMonth = monthlyValues.Sum(day => day.PlannedQuantity),
            ProducedToday = dailyValues
                .Where(day => day.Date == today)
                .Sum(day => day.ProducedQuantity),
            ProducedThisWeek = weeklyValues.Sum(day => day.ProducedQuantity),
            ProducedThisMonth = monthlyValues.Sum(day => day.ProducedQuantity)
        };
    }

    private static DailyProductionValue CreateDailyValue(DateOnly date)
    {
        var random = new StableDemoRandom(CreateSeed(date));
        var planned = random.Next(80, 181);

        return new DailyProductionValue(
            date,
            planned,
            random.Next(0, planned + 41));
    }

    private static uint CreateSeed(DateOnly date)
    {
        var seed = unchecked((uint)date.DayNumber * 2_246_822_519u);
        return seed == 0 ? 0x9E3779B9u : seed;
    }

    private readonly record struct DailyProductionValue(
        DateOnly Date,
        int PlannedQuantity,
        int ProducedQuantity);

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
