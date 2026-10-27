namespace ProductionPlanning.Web.ViewModels;

public class ProductDetailsViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public required ProductProductionStatisticsViewModel Statistics { get; init; }
}

public class ProductProductionStatisticsViewModel
{
    public long PlannedThisMonth { get; init; }

    public long ProducedForThisMonthPlans { get; init; }

    public long RemainingQuantity =>
        Math.Max(0, PlannedThisMonth - ProducedForThisMonthPlans);

    public bool IsOverproduced =>
        ProducedForThisMonthPlans > PlannedThisMonth;

    public double? CompletionPercentage =>
        PlannedThisMonth == 0
            ? null
            : ProducedForThisMonthPlans * 100d / PlannedThisMonth;

    public required IReadOnlyList<ProductPlanDailyStatisticsViewModel>
        PlanDailyValues { get; init; }

    public required IReadOnlyList<ProductRecordedDailyStatisticsViewModel>
        RecordedDailyValues { get; init; }

    public required IReadOnlyList<ProductPlanSummaryViewModel> RecentPlans
        { get; init; }
}

public class ProductPlanDailyStatisticsViewModel
{
    public DateOnly Date { get; init; }

    public long PlannedQuantity { get; init; }

    public long ProducedQuantity { get; init; }
}

public class ProductRecordedDailyStatisticsViewModel
{
    public DateOnly Date { get; init; }

    public long Quantity { get; init; }
}

public class ProductPlanSummaryViewModel
{
    public DateOnly PlannedDate { get; init; }

    public string MachineCode { get; init; } = string.Empty;

    public string MachineName { get; init; } = string.Empty;

    public long PlannedQuantity { get; init; }

    public long ProducedQuantity { get; init; }

    public double CompletionPercentage =>
        PlannedQuantity == 0 ? 0 : ProducedQuantity * 100d / PlannedQuantity;
}
