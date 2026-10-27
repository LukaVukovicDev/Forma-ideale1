using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Controllers;

public class OperationsController(ApplicationDbContext dbContext) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var operations = await dbContext.OperationDefinitions
            .AsNoTracking()
            .OrderBy(operation => operation.Name)
            .Select(operation => new OperationIndexItemViewModel
            {
                Id = operation.Id,
                Code = operation.Code,
                Name = operation.Name,
                Description = operation.Description,
                IsActive = operation.IsActive,
                CompatibleMachineCount = operation.MachineCapabilities.Count
            })
            .ToListAsync(cancellationToken);

        return View(operations);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateOperationViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateOperationViewModel viewModel,
        CancellationToken cancellationToken)
    {
        viewModel.Code = NormalizeCode(viewModel.Code);
        viewModel.Name = viewModel.Name?.Trim() ?? string.Empty;
        viewModel.Description = NormalizeDescription(viewModel.Description);
        ValidateNormalizedOperation(viewModel.Code, viewModel.Name, viewModel.Description);

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        if (await dbContext.OperationDefinitions.AnyAsync(
                operation => operation.Code == viewModel.Code,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(viewModel.Code), "Operacija sa ovom šifrom već postoji.");
            return View(viewModel);
        }

        dbContext.OperationDefinitions.Add(new OperationDefinition
        {
            Code = viewModel.Code,
            Name = viewModel.Name,
            Description = viewModel.Description,
            IsActive = viewModel.IsActive
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            ModelState.AddModelError(nameof(viewModel.Code), "Operacija sa ovom šifrom već postoji.");
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Operacija je uspešno dodata.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var operation = await dbContext.OperationDefinitions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (operation is null)
        {
            return NotFound();
        }

        return View(new EditOperationViewModel
        {
            Id = operation.Id,
            Code = operation.Code,
            Name = operation.Name,
            Description = operation.Description,
            IsActive = operation.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditOperationViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        viewModel.Code = NormalizeCode(viewModel.Code);
        viewModel.Name = viewModel.Name?.Trim() ?? string.Empty;
        viewModel.Description = NormalizeDescription(viewModel.Description);
        ValidateNormalizedOperation(viewModel.Code, viewModel.Name, viewModel.Description);

        var operation = await dbContext.OperationDefinitions
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (operation is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        if (await dbContext.OperationDefinitions.AnyAsync(
                item => item.Id != id && item.Code == viewModel.Code,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(viewModel.Code), "Operacija sa ovom šifrom već postoji.");
            return View(viewModel);
        }

        operation.Code = viewModel.Code;
        operation.Name = viewModel.Name;
        operation.Description = viewModel.Description;
        operation.IsActive = viewModel.IsActive;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            ModelState.AddModelError(nameof(viewModel.Code), "Operacija sa ovom šifrom već postoji.");
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Podaci o operaciji su uspešno izmenjeni.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var operation = await dbContext.OperationDefinitions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return operation is null ? NotFound() : View(operation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(
        int id,
        CancellationToken cancellationToken)
    {
        var operation = await dbContext.OperationDefinitions
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (operation is null)
        {
            return NotFound();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await dbContext.ProductOperations.AnyAsync(
                step => step.OperationDefinitionId == id,
                cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            TempData["ErrorMessage"] = "Operacija je deo rute proizvoda i ne može se obrisati. Možete je deaktivirati.";
            return RedirectToAction(nameof(Index));
        }

        var capabilities = await dbContext.MachineOperationCapabilities
            .Where(item => item.OperationDefinitionId == id)
            .ToListAsync(cancellationToken);
        dbContext.MachineOperationCapabilities.RemoveRange(capabilities);
        dbContext.OperationDefinitions.Remove(operation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["SuccessMessage"] = "Operacija je obrisana.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var operation = await dbContext.OperationDefinitions
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.Code,
                item.Name,
                item.Description,
                item.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (operation is null)
        {
            return NotFound();
        }

        var selectedIds = await dbContext.MachineOperationCapabilities
            .AsNoTracking()
            .Where(capability => capability.OperationDefinitionId == id)
            .Select(capability => capability.MachineId)
            .ToListAsync(cancellationToken);

        var machines = await dbContext.Machines
            .AsNoTracking()
            .OrderBy(machine => machine.Naziv)
            .Select(machine => new OperationMachineSelectionViewModel
            {
                Id = machine.Id,
                Code = machine.SifraMasine,
                Name = machine.Naziv,
                IsActive = machine.IsActive,
                IsSelected = selectedIds.Contains(machine.Id)
            })
            .ToListAsync(cancellationToken);

        return View(new OperationDetailsViewModel
        {
            Id = operation.Id,
            Code = operation.Code,
            Name = operation.Name,
            Description = operation.Description,
            IsActive = operation.IsActive,
            Machines = machines
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCapabilities(
        SaveOperationCapabilitiesViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var operationExists = await dbContext.OperationDefinitions.AnyAsync(
            operation => operation.Id == viewModel.OperationDefinitionId,
            cancellationToken);
        if (!operationExists)
        {
            return NotFound();
        }

        var selectedIds = viewModel.MachineIds.Distinct().ToArray();
        var existingMachineCount = await dbContext.Machines.CountAsync(
            machine => selectedIds.Contains(machine.Id),
            cancellationToken);
        if (existingMachineCount != selectedIds.Length)
        {
            return BadRequest("Izabrana je nepostojeća mašina.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var current = await dbContext.MachineOperationCapabilities
            .Where(capability => capability.OperationDefinitionId == viewModel.OperationDefinitionId)
            .ToListAsync(cancellationToken);
        var currentIds = current.Select(capability => capability.MachineId).ToHashSet();
        dbContext.MachineOperationCapabilities.RemoveRange(
            current.Where(capability => !selectedIds.Contains(capability.MachineId)));
        dbContext.MachineOperationCapabilities.AddRange(
            selectedIds
                .Where(machineId => !currentIds.Contains(machineId))
                .Select(machineId => new MachineOperationCapability
                {
                    OperationDefinitionId = viewModel.OperationDefinitionId,
                    MachineId = machineId
                }));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            TempData["ErrorMessage"] = "Kompatibilnost mašina je promenjena u drugom zahtevu. Osvežite stranicu i pokušajte ponovo.";
            return RedirectToAction(nameof(Details), new { id = viewModel.OperationDefinitionId });
        }

        TempData["SuccessMessage"] = "Kompatibilnost mašina je sačuvana.";
        return RedirectToAction(nameof(Details), new { id = viewModel.OperationDefinitionId });
    }

    private static string NormalizeCode(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();

    private static string? NormalizeDescription(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private void ValidateNormalizedOperation(string code, string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            ModelState.AddModelError(nameof(CreateOperationViewModel.Code), "Šifra operacije je obavezna.");
        }

        if (code.Length > 40)
        {
            ModelState.AddModelError(nameof(CreateOperationViewModel.Code), "Šifra može imati najviše 40 karaktera.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(CreateOperationViewModel.Name), "Naziv operacije je obavezan.");
        }

        if (name.Length > 200)
        {
            ModelState.AddModelError(nameof(CreateOperationViewModel.Name), "Naziv može imati najviše 200 karaktera.");
        }

        if (description?.Length > 1000)
        {
            ModelState.AddModelError(nameof(CreateOperationViewModel.Description), "Opis može imati najviše 1000 karaktera.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            SqliteExtendedErrorCode: 2067 or 1555
        };
}
