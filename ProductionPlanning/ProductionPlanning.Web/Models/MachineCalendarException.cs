namespace ProductionPlanning.Web.Models;

public class MachineCalendarException
{
    public int Id { get; set; }

    public int MachineId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public MachineCalendarExceptionType ExceptionType { get; set; }

    public string? Reason { get; set; }

    public Machine Machine { get; set; } = null!;
}
