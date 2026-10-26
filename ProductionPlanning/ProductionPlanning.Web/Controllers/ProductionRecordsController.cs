using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Controllers;

public class ProductionRecordsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public ProductionRecordsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var records = await _dbContext.ProductionRecords
            .AsNoTracking()
            .OrderByDescending(record => record.RecordedAtUtc)
            .ThenByDescending(record => record.Id)
            .Select(record => new ProductionRecordIndexViewModel
            {
                Id = record.Id,
                ProductCode = record.PlannedProduction.Product.SifraProizvoda,
                ProductName = record.PlannedProduction.Product.Naziv,
                MachineCode = record.PlannedProduction.Machine.SifraMasine,
                MachineName = record.PlannedProduction.Machine.Naziv,
                PlannedDate = record.PlannedProduction.PlannedDate,
                Quantity = record.Quantity,
                RecordedAtUtc = record.RecordedAtUtc
            })
            .ToListAsync(cancellationToken);

        return View(records);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        int? plannedProductionId,
        CancellationToken cancellationToken)
    {
        var model = new CreateProductionRecordViewModel
        {
            PlannedProductionId = plannedProductionId ?? 0
        };
        await PopulateSelectListAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateProductionRecordViewModel model,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid &&
            !await _dbContext.PlannedProductions.AnyAsync(
                plan => plan.Id == model.PlannedProductionId,
                cancellationToken))
        {
            ModelState.AddModelError(
                nameof(model.PlannedProductionId),
                "Izabrani plan proizvodnje ne postoji.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListAsync(model, cancellationToken);
            return View(model);
        }

        _dbContext.ProductionRecords.Add(new ProductionRecord
        {
            PlannedProductionId = model.PlannedProductionId,
            Quantity = model.Quantity,
            RecordedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Realizacija proizvodnje je evidentirana.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectListAsync(
        CreateProductionRecordViewModel model,
        CancellationToken cancellationToken)
    {
        var plans = await _dbContext.PlannedProductions
            .AsNoTracking()
            .OrderByDescending(plan => plan.PlannedDate)
            .ThenBy(plan => plan.Id)
            .Select(plan => new
            {
                plan.Id,
                ProductCode = plan.Product.SifraProizvoda,
                ProductName = plan.Product.Naziv,
                MachineCode = plan.Machine.SifraMasine,
                MachineName = plan.Machine.Naziv,
                plan.PlannedDate
            })
            .ToListAsync(cancellationToken);

        model.PlannedProductions = plans.Select(plan => new SelectListItem
        {
            Value = plan.Id.ToString(),
            Text = $"{plan.ProductCode} — {plan.ProductName} | " +
                   $"{plan.MachineCode} — {plan.MachineName} | " +
                   plan.PlannedDate.ToString("dd.MM.yyyy."),
            Selected = plan.Id == model.PlannedProductionId
        }).ToList();
    }
}
