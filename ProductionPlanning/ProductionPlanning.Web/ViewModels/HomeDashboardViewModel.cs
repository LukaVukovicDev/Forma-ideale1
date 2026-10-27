namespace ProductionPlanning.Web.ViewModels;

public class HomeDashboardViewModel
{
    public required DashboardProductionStatisticsViewModel Statistics { get; init; }
}

public class DashboardProductionStatisticsViewModel
{
    public DateOnly Today { get; init; }

    public int PlannedThisMonth { get; init; }

    public int ProducedToday { get; init; }

    public int ProducedThisWeek { get; init; }

    public int ProducedThisMonth { get; init; }

    public double? PlanSuccessPercentage =>
        PlannedThisMonth == 0
            ? null
            : ProducedThisMonth * 100d / PlannedThisMonth;
}
