namespace ProductionPlanning.Web.Models;

public class Machine
{
    public int Id { get; set; }

    public string SifraMasine { get; set; } = string.Empty;

    public string Naziv { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<PlannedProduction> PlannedProductions { get; set; } =
        new List<PlannedProduction>();

    public ICollection<MachineOperationCapability> OperationCapabilities { get; set; } =
        new List<MachineOperationCapability>();

    public ICollection<MachineWorkingInterval> WorkingIntervals { get; set; } =
        new List<MachineWorkingInterval>();

    public ICollection<MachineBreak> Breaks { get; set; } =
        new List<MachineBreak>();

    public ICollection<MachineCalendarException> CalendarExceptions { get; set; } =
        new List<MachineCalendarException>();

    public ICollection<ScheduledProductionOperation> ScheduledOperations { get; set; } =
        new List<ScheduledProductionOperation>();
}
