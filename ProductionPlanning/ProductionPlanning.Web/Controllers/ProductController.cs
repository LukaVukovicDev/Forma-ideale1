using Microsoft.AspNetCore.Mvc;
using ProductionPlanning.Web.Models;

namespace ProductionPlanning.Web.Controllers;

public class ProductsController : Controller
{
    public IActionResult Index()
    {
        var products = new List<Product>
        {
            new Product
            {
                Id = 1,
                SifraProizvoda = "ORM-001",
                Naziv = "Ormar Verona"
            },
            new Product
            {
                Id = 2,
                SifraProizvoda = "STO-001",
                Naziv = "Radni sto"
            },
            new Product
            {
                Id = 3,
                SifraProizvoda = "KOM-001",
                Naziv = "Komoda"
            }
        };

        return View(products);
    }
}