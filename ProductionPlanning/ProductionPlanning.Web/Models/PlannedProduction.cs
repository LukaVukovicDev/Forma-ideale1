namespace ProductionPlanning.Web.Models;

public class PlannedProduction
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int MachineId { get; set; }

    public DateOnly PlannedDate { get; set; }

    public int PlannedQuantity { get; set; }

    public Product Product { get; set; } = null!;

    public Machine Machine { get; set; } = null!;

    public ICollection<ProductionRecord> ProductionRecords { get; set; } =
        new List<ProductionRecord>();
}
