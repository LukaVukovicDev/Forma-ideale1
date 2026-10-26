using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public ProductsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.SifraProizvoda)
            .ToListAsync(cancellationToken);

        return View(products);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateProductViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateProductViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var sifra = model.SifraProizvoda.Trim().ToUpperInvariant();
        var naziv = model.Naziv.Trim();

        if (string.IsNullOrWhiteSpace(sifra))
        {
            ModelState.AddModelError(
                nameof(model.SifraProizvoda),
                "Šifra proizvoda je obavezna.");
        }

        if (string.IsNullOrWhiteSpace(naziv))
        {
            ModelState.AddModelError(
                nameof(model.Naziv),
                "Naziv proizvoda je obavezan.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var postoji = await _dbContext.Products.AnyAsync(
            p => p.SifraProizvoda == sifra,
            cancellationToken);

        if (postoji)
        {
            ModelState.AddModelError(
                nameof(model.SifraProizvoda),
                "Proizvod sa ovom šifrom već postoji.");

            return View(model);
        }

        var product = new Product
        {
            SifraProizvoda = sifra,
            Naziv = naziv
        };

        _dbContext.Products.Add(product);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException
                  { SqliteExtendedErrorCode: 2067 })
        {
            ModelState.AddModelError(
                nameof(model.SifraProizvoda),
                "Proizvod sa ovom šifrom već postoji.");

            return View(model);
        }

        TempData["SuccessMessage"] = "Proizvod je uspešno dodat.";

        return RedirectToAction(nameof(Index));
    }
}