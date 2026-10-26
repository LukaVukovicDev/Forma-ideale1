namespace ProductionPlanning.Web.ViewModels;

public class ProductionRecordIndexViewModel
{
    public int Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string MachineCode { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public DateOnly PlannedDate { get; set; }

    public int Quantity { get; set; }

    public DateTime RecordedAtUtc { get; set; }
}
