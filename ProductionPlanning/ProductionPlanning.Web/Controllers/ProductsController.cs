using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.Services;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ProductStatisticsService _statisticsService;
    private readonly ProductRouteService _routeService;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        ApplicationDbContext dbContext,
        ProductStatisticsService statisticsService,
        ProductRouteService routeService,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _statisticsService = statisticsService;
        _routeService = routeService;
        _environment = environment;
    }

    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderByDescending(product => product.Id)
            .ToListAsync(cancellationToken);

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        var statistics = await _statisticsService.GetStatisticsAsync(
            product.Id,
            cancellationToken);
        var route = await _routeService.GetRouteAsync(product.Id, cancellationToken);
        if (route is null)
        {
            return NotFound();
        }

        return View(new ProductDetailsViewModel
        {
            Id = product.Id,
            Name = product.Naziv,
            Code = product.SifraProizvoda,
            ImagePath = GetProductImagePath(product.SifraProizvoda),
            Statistics = statistics,
            Route = route
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateProductViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return View(new EditProductViewModel
        {
            Id = product.Id,
            SifraProizvoda = product.SifraProizvoda,
            Naziv = product.Naziv
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditProductViewModel model,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        if (model.Id != id)
        {
            ModelState.AddModelError(
                nameof(model.Id),
                "Identifikator proizvoda nije ispravan.");
        }

        var code = model.SifraProizvoda?.Trim().ToUpperInvariant() ?? string.Empty;
        var name = model.Naziv?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(code))
        {
            ModelState.AddModelError(
                nameof(model.SifraProizvoda),
                "Šifra proizvoda je obavezna.");
        }
        else if (code.Length > 40)
        {
            ModelState.AddModelError(
                nameof(model.SifraProizvoda),
                "Šifra proizvoda može imati najviše 40 karaktera.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(
                nameof(model.Naziv),
                "Naziv proizvoda je obavezan.");
        }
        else if (name.Length > 200)
        {
            ModelState.AddModelError(
                nameof(model.Naziv),
                "Naziv proizvoda može imati najviše 200 karaktera.");
        }

        if (ModelState.IsValid &&
            await _dbContext.Products.AnyAsync(
                item => item.Id != id && item.SifraProizvoda == code,
                cancellationToken))
        {
            ModelState.AddModelError(
                nameof(model.SifraProizvoda),
                "Proizvod sa ovom šifrom već postoji.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        product.SifraProizvoda = code;
        product.Naziv = name;

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

        TempData["SuccessMessage"] = "Podaci o proizvodu su uspešno izmenjeni.";
        return RedirectToAction(nameof(Index));
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

    private string GetProductImagePath(string productCode)
    {
        var fileName = new string(productCode
            .Where(character =>
                char.IsAsciiLetterOrDigit(character) ||
                character is '-' or '_')
            .ToArray());

        var imagesDirectory = Path.Combine(
            _environment.WebRootPath,
            "images",
            "products");

        foreach (var extension in new[] { ".jpg", ".jpeg", ".png" })
        {
            var imageName = fileName + extension;
            if (fileName.Length > 0 &&
                System.IO.File.Exists(Path.Combine(imagesDirectory, imageName)))
            {
                return Url.Content(
                    $"~/images/products/{Uri.EscapeDataString(imageName)}");
            }
        }

        return Url.Content("~/images/products/product-placeholder.svg");
    }
}