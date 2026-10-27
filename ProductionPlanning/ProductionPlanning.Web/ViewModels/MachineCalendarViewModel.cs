using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.Services;

namespace ProductionPlanning.Web.ViewModels;

public sealed class MachineCalendarViewModel
{
    public int MachineId { get; set; }

    public string MachineName { get; set; } = string.Empty;

    public string MachineCode { get; set; } = string.Empty;

    public DateOnly SelectedDate { get; set; }

    public int AvailableMinutes { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public string? AvailabilityError { get; set; }

    public IReadOnlyList<MachineAvailabilityInterval> EffectiveIntervals { get; set; } =
        [];

    public IReadOnlyList<MachineAvailabilitySegment> Timeline { get; set; } = [];

    public IReadOnlyList<MachineWorkingInterval> WorkingIntervals { get; set; } = [];

    public IReadOnlyList<MachineBreak> Breaks { get; set; } = [];

    public IReadOnlyList<MachineCalendarException> Exceptions { get; set; } = [];

    public DayOfWeek WorkingDay { get; set; }

    public TimeOnly WorkingStart { get; set; } = new(8, 0);

    public TimeOnly WorkingEnd { get; set; } = new(16, 0);

    public DayOfWeek BreakDay { get; set; }

    public TimeOnly BreakStart { get; set; } = new(12, 0);

    public TimeOnly BreakEnd { get; set; } = new(12, 30);

    public string? BreakDescription { get; set; }

    public DateOnly ExceptionDate { get; set; }

    public TimeOnly ExceptionStart { get; set; } = new(8, 0);

    public TimeOnly ExceptionEnd { get; set; } = new(16, 0);

    public MachineCalendarExceptionType ExceptionType { get; set; }

    public string? ExceptionReason { get; set; }
}
