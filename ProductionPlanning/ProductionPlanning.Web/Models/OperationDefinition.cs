namespace ProductionPlanning.Web.Models;

public class OperationDefinition
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ProductOperation> ProductOperations { get; set; } =
        new List<ProductOperation>();

    public ICollection<MachineOperationCapability> MachineCapabilities { get; set; } =
        new List<MachineOperationCapability>();
}
