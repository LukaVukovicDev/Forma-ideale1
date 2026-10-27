namespace ProductionPlanning.Web.Models;

public class Product
{
    public int Id { get; set; }

    public string SifraProizvoda { get; set; } = string.Empty;

    public string Naziv { get; set; } = string.Empty;

    public ICollection<ProductOperation> ProductOperations { get; set; } =
        new List<ProductOperation>();
}