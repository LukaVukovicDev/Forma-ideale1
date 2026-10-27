using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Controllers;

public class PlannedProductionsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public PlannedProductionsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var plans = await _dbContext.PlannedProductions
            .AsNoTracking()
            .OrderByDescending(plan => plan.Id)
            .Select(plan => new PlannedProductionIndexViewModel
            {
                Id = plan.Id,
                ProductCode = plan.Product.SifraProizvoda,
                ProductName = plan.Product.Naziv,
                MachineCode = plan.Machine.SifraMasine,
                MachineName = plan.Machine.Naziv,
                PlannedDate = plan.PlannedDate,
                PlannedQuantity = plan.PlannedQuantity,
                TotalProduced = plan.ProductionRecords
                    .Sum(record => (long?)record.Quantity) ?? 0
            })
            .ToListAsync(cancellationToken);

        return View(plans);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var model = new CreatePlannedProductionViewModel
        {
            PlannedDate = DateOnly.FromDateTime(DateTime.Today)
        };
        await PopulateSelectListsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreatePlannedProductionViewModel model,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            if (!await _dbContext.Products.AnyAsync(
                    product => product.Id == model.ProductId,
                    cancellationToken))
            {
                ModelState.AddModelError(
                    nameof(model.ProductId),
                    "Izabrani proizvod ne postoji.");
            }

            if (!await _dbContext.Machines.AnyAsync(
                    machine => machine.Id == model.MachineId && machine.IsActive,
                    cancellationToken))
            {
                ModelState.AddModelError(
                    nameof(model.MachineId),
                    "Izabrana mašina ne postoji ili nije aktivna.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        _dbContext.PlannedProductions.Add(new PlannedProduction
        {
            ProductId = model.ProductId,
            MachineId = model.MachineId,
            PlannedDate = model.PlannedDate!.Value,
            PlannedQuantity = model.PlannedQuantity
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Plan proizvodnje je uspešno dodat.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectListsAsync(
        CreatePlannedProductionViewModel model,
        CancellationToken cancellationToken)
    {
        model.Products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.SifraProizvoda)
            .Select(product => new SelectListItem
            {
                Value = product.Id.ToString(),
                Text = product.SifraProizvoda + " — " + product.Naziv,
                Selected = product.Id == model.ProductId
            })
            .ToListAsync(cancellationToken);

        model.Machines = await _dbContext.Machines
            .AsNoTracking()
            .Where(machine => machine.IsActive)
            .OrderBy(machine => machine.SifraMasine)
            .Select(machine => new SelectListItem
            {
                Value = machine.Id.ToString(),
                Text = machine.SifraMasine + " — " + machine.Naziv,
                Selected = machine.Id == model.MachineId
            })
            .ToListAsync(cancellationToken);
    }
}
