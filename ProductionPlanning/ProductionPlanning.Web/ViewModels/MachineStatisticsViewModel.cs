namespace ProductionPlanning.Web.ViewModels;

public class MachineStatisticsViewModel
{
    public required IReadOnlyList<MachineDailyStatisticsViewModel> DailyValues { get; init; }

    public long PlannedQuantity => DailyValues.Sum(day => (long)day.PlannedQuantity);

    public long ProducedQuantity => DailyValues.Sum(day => (long)day.ProducedQuantity);

    public long DowntimeMinutes => DailyValues.Sum(day => (long)day.DowntimeMinutes);

    public double CompletionPercentage =>
        PlannedQuantity == 0 ? 0 : ProducedQuantity * 100d / PlannedQuantity;
}

public class MachineDailyStatisticsViewModel
{
    public DateOnly Date { get; init; }

    public int PlannedQuantity { get; init; }

    public int ProducedQuantity { get; init; }

    public int DowntimeMinutes { get; init; }

    public IReadOnlyList<MachineDowntimeIntervalViewModel> DowntimeIntervals { get; init; } =
        [];
}

public class MachineDowntimeIntervalViewModel
{
    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }

    public int DurationMinutes { get; init; }
}
