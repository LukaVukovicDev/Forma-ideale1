namespace ProductionPlanning.Web.ViewModels;

public class MachineDetailsViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public string ImagePath { get; init; } = string.Empty;

    public required MachineStatisticsViewModel Statistics { get; init; }
}
