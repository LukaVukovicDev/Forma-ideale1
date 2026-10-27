namespace ProductionPlanning.Web.Models;

public class PlannedProduction
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    // Kept as the concrete machine assigned to the plan; future scheduling must
    // validate that assignment against the product route's machine capabilities.
    public int MachineId { get; set; }

    public DateOnly PlannedDate { get; set; }

    public int PlannedQuantity { get; set; }

    public Product Product { get; set; } = null!;

    public Machine Machine { get; set; } = null!;

    public ICollection<ProductionRecord> ProductionRecords { get; set; } =
        new List<ProductionRecord>();

    public ICollection<ScheduledProductionOperation> ScheduledOperations { get; set; } =
        new List<ScheduledProductionOperation>();
}
