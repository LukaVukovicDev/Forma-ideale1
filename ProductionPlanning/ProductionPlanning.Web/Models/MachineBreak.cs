namespace ProductionPlanning.Web.Models;

public class MachineBreak
{
    public int Id { get; set; }

    public int MachineId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Description { get; set; }

    public Machine Machine { get; set; } = null!;
}
