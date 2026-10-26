namespace ProductionPlanning.Web.Models;

public class ProductionRecord
{
    public int Id { get; set; }

    public int PlannedProductionId { get; set; }

    public int Quantity { get; set; }

    public DateTime RecordedAtUtc { get; set; }

    public PlannedProduction PlannedProduction { get; set; } = null!;
}
