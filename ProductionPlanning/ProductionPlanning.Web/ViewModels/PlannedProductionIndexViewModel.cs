namespace ProductionPlanning.Web.ViewModels;

public class PlannedProductionIndexViewModel
{
    public int Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string MachineCode { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public DateOnly PlannedDate { get; set; }

    public int PlannedQuantity { get; set; }

    public long TotalProduced { get; set; }

    public long RemainingQuantity =>
        Math.Max(0L, PlannedQuantity - TotalProduced);

    public double CompletionPercentage =>
        PlannedQuantity == 0 ? 0 : TotalProduced * 100d / PlannedQuantity;

    public bool IsOverproduced => TotalProduced > PlannedQuantity;
}
