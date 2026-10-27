namespace ProductionPlanning.Web.ViewModels;

public sealed class ProductionSchedulingViewModel
{
    public DateOnly SelectedDate { get; set; }

    public IReadOnlyList<UnscheduledOperationViewModel> UnscheduledOperations { get; set; } = [];

    public IReadOnlyList<SchedulingMachineOptionViewModel> Machines { get; set; } = [];

    public IReadOnlyList<ScheduledOperationViewModel> ScheduledOperations { get; set; } = [];

    public IReadOnlyList<SchedulingMachineRowViewModel> MachineRows { get; set; } = [];
}

public sealed class UnscheduledOperationViewModel
{
    public int PlannedProductionId { get; set; }

    public int ProductOperationId { get; set; }

    public int OperationDefinitionId { get; set; }

    public int SequenceNumber { get; set; }

    public string OperationName { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateOnly PlannedDate { get; set; }

    public long DurationSeconds { get; set; }

    public bool CanSchedule { get; set; }

    public string? SchedulingBlockReason { get; set; }

    public string DurationLabel
    {
        get
        {
            var duration = TimeSpan.FromSeconds(DurationSeconds);
            return duration.Days > 0
                ? $"{duration.Days} d {duration:hh\\:mm\\:ss}"
                : duration.ToString(@"hh\:mm\:ss");
        }
    }
}

public sealed class SchedulingMachineOptionViewModel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<int> OperationDefinitionIds { get; set; } = [];
}

public sealed class ScheduledOperationViewModel
{
    public int Id { get; set; }

    public int PlannedProductionId { get; set; }

    public int ProductOperationId { get; set; }

    public int MachineId { get; set; }

    public string MachineCode { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string OperationName { get; set; } = string.Empty;

    public int SequenceNumber { get; set; }

    public DateTime StartAtLocal { get; set; }

    public DateTime EndAtLocal { get; set; }

    public long DurationMinutes => (long)Math.Ceiling((EndAtLocal - StartAtLocal).TotalMinutes);

    public double LeftPercent { get; set; }

    public double WidthPercent { get; set; }

    public string Color { get; set; } = "#4b83b6";
}

public sealed class SchedulingMachineRowViewModel
{
    public int MachineId { get; set; }

    public string MachineCode { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public IReadOnlyList<ScheduledOperationViewModel> Operations { get; set; } = [];
}
