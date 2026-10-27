namespace ProductionPlanning.Web.Models;

public class MachineOperationCapability
{
    public int Id { get; set; }

    public int MachineId { get; set; }

    public int OperationDefinitionId { get; set; }

    public Machine Machine { get; set; } = null!;

    public OperationDefinition OperationDefinition { get; set; } = null!;
}
