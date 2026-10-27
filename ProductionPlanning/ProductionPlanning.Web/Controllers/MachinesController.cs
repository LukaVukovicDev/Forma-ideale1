using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.Services;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Controllers;

public class MachinesController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly MachineStatisticsService _statisticsService;
    private readonly IWebHostEnvironment _environment;

    public MachinesController(
        ApplicationDbContext dbContext,
        MachineStatisticsService statisticsService,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _statisticsService = statisticsService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var machines = await _dbContext.Machines
            .AsNoTracking()
            .OrderByDescending(machine => machine.Id)
            .ToListAsync(cancellationToken);

        return View(machines);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var machine = await _dbContext.Machines
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (machine is null)
        {
            return NotFound();
        }

        var imagePath = GetMachineImagePath(machine.Id);
        var statistics = _statisticsService.GetStatistics(
            machine.Id,
            DateOnly.FromDateTime(DateTime.Today));

        return View(new MachineDetailsViewModel
        {
            Id = machine.Id,
            Name = machine.Naziv,
            Code = machine.SifraMasine,
            IsActive = machine.IsActive,
            ImagePath = imagePath,
            Statistics = statistics
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateMachineViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        var machine = await _dbContext.Machines
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (machine is null)
        {
            return NotFound();
        }

        return View(new EditMachineViewModel
        {
            Id = machine.Id,
            SifraMasine = machine.SifraMasine,
            Naziv = machine.Naziv,
            IsActive = machine.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditMachineViewModel model,
        CancellationToken cancellationToken)
    {
        var machine = await _dbContext.Machines
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (machine is null)
        {
            return NotFound();
        }

        if (model.Id != id)
        {
            ModelState.AddModelError(
                nameof(model.Id),
                "Identifikator mašine nije ispravan.");
        }

        var code = model.SifraMasine?.Trim().ToUpperInvariant() ?? string.Empty;
        var name = model.Naziv?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(code))
        {
            ModelState.AddModelError(
                nameof(model.SifraMasine),
                "Šifra mašine je obavezna.");
        }
        else if (code.Length > 40)
        {
            ModelState.AddModelError(
                nameof(model.SifraMasine),
                "Šifra mašine može imati najviše 40 karaktera.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(
                nameof(model.Naziv),
                "Naziv mašine je obavezan.");
        }
        else if (name.Length > 200)
        {
            ModelState.AddModelError(
                nameof(model.Naziv),
                "Naziv mašine može imati najviše 200 karaktera.");
        }

        if (ModelState.IsValid &&
            await _dbContext.Machines.AnyAsync(
                item => item.Id != id && item.SifraMasine == code,
                cancellationToken))
        {
            ModelState.AddModelError(
                nameof(model.SifraMasine),
                "Mašina sa ovom šifrom već postoji.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        machine.SifraMasine = code;
        machine.Naziv = name;
        machine.IsActive = model.IsActive;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException
                  { SqliteExtendedErrorCode: 2067 })
        {
            ModelState.AddModelError(
                nameof(model.SifraMasine),
                "Mašina sa ovom šifrom već postoji.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Podaci o mašini su uspešno izmenjeni.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateMachineViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var code = model.SifraMasine.Trim().ToUpperInvariant();
        var name = model.Naziv.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            ModelState.AddModelError(
                nameof(model.SifraMasine),
                "Šifra mašine je obavezna.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(
                nameof(model.Naziv),
                "Naziv mašine je obavezan.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _dbContext.Machines.AnyAsync(
                machine => machine.SifraMasine == code,
                cancellationToken))
        {
            ModelState.AddModelError(
                nameof(model.SifraMasine),
                "Mašina sa ovom šifrom već postoji.");
            return View(model);
        }

        _dbContext.Machines.Add(new Machine
        {
            SifraMasine = code,
            Naziv = name,
            IsActive = model.IsActive
        });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException
                  { SqliteExtendedErrorCode: 2067 })
        {
            ModelState.AddModelError(
                nameof(model.SifraMasine),
                "Mašina sa ovom šifrom već postoji.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Mašina je uspešno dodata.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(
        int id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var machine = await _dbContext.Machines
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (machine is null)
        {
            return NotFound();
        }

        machine.IsActive = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = isActive
            ? "Mašina je aktivirana za nove planove."
            : "Mašina je deaktivirana za nove planove.";
        return RedirectToAction(nameof(Index));
    }

    private string GetMachineImagePath(int machineId)
    {
        var imagesDirectory = Path.Combine(
            _environment.WebRootPath,
            "images",
            "machines");

        foreach (var extension in new[] { ".webp", ".jpg", ".jpeg", ".png" })
        {
            var fileName = $"machine-{machineId}{extension}";
            if (System.IO.File.Exists(Path.Combine(imagesDirectory, fileName)))
            {
                return Url.Content($"~/images/machines/{fileName}");
            }
        }

        return Url.Content("~/images/machines/machine-placeholder.svg");
    }
}
