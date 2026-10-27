namespace ProductionPlanning.Web.Models;

public class ScheduledProductionOperation
{
    public int Id { get; set; }

    public int PlannedProductionId { get; set; }

    public int ProductOperationId { get; set; }

    public int MachineId { get; set; }

    public DateTime StartAtUtc { get; set; }

    public DateTime EndAtUtc { get; set; }

    public PlannedProduction PlannedProduction { get; set; } = null!;

    public ProductOperation ProductOperation { get; set; } = null!;

    public Machine Machine { get; set; } = null!;
}
