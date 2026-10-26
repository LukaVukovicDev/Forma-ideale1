namespace ProductionPlanning.Web.Models;

public class Machine
{
    public int Id { get; set; }

    public string SifraMasine { get; set; } = string.Empty;

    public string Naziv { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<PlannedProduction> PlannedProductions { get; set; } =
        new List<PlannedProduction>();
}
