using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.Services;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Controllers;

public class ProductRoutesController(
    ApplicationDbContext dbContext,
    ProductRouteService routeService,
    RouteCalculationService calculationService,
    RouteSequenceService sequenceService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var viewModel = await routeService.GetEditorAsync(id, cancellationToken: cancellationToken);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStep(
        AddProductRouteStepViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Products.AnyAsync(
                product => product.Id == viewModel.ProductId,
                cancellationToken))
        {
            return NotFound();
        }

        var operation = await dbContext.OperationDefinitions
            .SingleOrDefaultAsync(
                item => item.Id == viewModel.OperationDefinitionId && item.IsActive,
                cancellationToken);
        if (operation is null)
        {
            ModelState.AddModelError(
                nameof(viewModel.OperationDefinitionId),
                "Izaberite postojeću aktivnu operaciju.");
        }

        if (!ModelState.IsValid)
        {
            return await EditorView(viewModel.ProductId, cancellationToken);
        }

        var lastSequence = await dbContext.ProductOperations
            .Where(step => step.ProductId == viewModel.ProductId)
            .Select(step => (int?)step.SequenceNumber)
            .MaxAsync(cancellationToken) ?? 0;
        if (lastSequence == int.MaxValue)
        {
            ModelState.AddModelError(string.Empty, "Nije moguće dodati još koraka ovoj ruti.");
            return await EditorView(viewModel.ProductId, cancellationToken);
        }

        dbContext.ProductOperations.Add(new ProductOperation
        {
            ProductId = viewModel.ProductId,
            OperationDefinitionId = operation!.Id,
            SequenceNumber = lastSequence + 1,
            SetupDurationMinutes = viewModel.SetupDurationMinutes,
            ProcessingSecondsPerUnit = viewModel.ProcessingSecondsPerUnit
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is Microsoft.Data.Sqlite.SqliteException
                  { SqliteErrorCode: 19 })
        {
            ModelState.AddModelError(
                string.Empty,
                "Korak rute nije sačuvan. Osvežite stranicu i pokušajte ponovo.");
            return await EditorView(viewModel.ProductId, cancellationToken);
        }

        TempData["SuccessMessage"] = "Korak je dodat u rutu proizvoda.";
        return RedirectToAction(nameof(Edit), new { id = viewModel.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStep(
        EditProductRouteStepViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var step = await dbContext.ProductOperations
            .SingleOrDefaultAsync(
                item => item.Id == viewModel.StepId && item.ProductId == viewModel.ProductId,
                cancellationToken);
        if (step is null)
        {
            return NotFound();
        }

        var targetOperation = await dbContext.OperationDefinitions
            .SingleOrDefaultAsync(
                item => item.Id == viewModel.OperationDefinitionId,
                cancellationToken);
        if (targetOperation is null
            || (!targetOperation.IsActive
                && targetOperation.Id != step.OperationDefinitionId))
        {
            ModelState.AddModelError(
                nameof(viewModel.OperationDefinitionId),
                "Izaberite postojeću aktivnu operaciju.");
        }

        if (!ModelState.IsValid)
        {
            return await EditorView(viewModel.ProductId, cancellationToken);
        }

        step.OperationDefinitionId = targetOperation!.Id;
        step.SetupDurationMinutes = viewModel.SetupDurationMinutes;
        step.ProcessingSecondsPerUnit = viewModel.ProcessingSecondsPerUnit;
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Vremena operacije su ažurirana.";
        return RedirectToAction(nameof(Edit), new { id = viewModel.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeOperation(
        ChangeProductRouteOperationViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var step = await dbContext.ProductOperations
            .SingleOrDefaultAsync(
                item => item.Id == viewModel.StepId && item.ProductId == viewModel.ProductId,
                cancellationToken);
        if (step is null)
        {
            return NotFound();
        }

        var operation = await dbContext.OperationDefinitions
            .SingleOrDefaultAsync(
                item => item.Id == viewModel.OperationDefinitionId && item.IsActive,
                cancellationToken);
        if (operation is null)
        {
            ModelState.AddModelError(
                nameof(viewModel.OperationDefinitionId),
                "Izaberite postojeću aktivnu operaciju.");
        }

        if (!ModelState.IsValid)
        {
            return await EditorView(viewModel.ProductId, cancellationToken);
        }

        step.OperationDefinitionId = operation!.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Operacija u koraku je izmenjena.";
        return RedirectToAction(nameof(Edit), new { id = viewModel.ProductId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> MoveUp(
        int productId,
        int stepId,
        CancellationToken cancellationToken) =>
        MoveStepAsync(productId, stepId, -1, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> MoveDown(
        int productId,
        int stepId,
        CancellationToken cancellationToken) =>
        MoveStepAsync(productId, stepId, 1, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStep(
        int productId,
        int stepId,
        CancellationToken cancellationToken)
    {
        var allSteps = await dbContext.ProductOperations
            .Where(item => item.ProductId == productId)
            .OrderBy(item => item.SequenceNumber)
            .ToListAsync(cancellationToken);
        var step = allSteps.SingleOrDefault(item => item.Id == stepId);
        if (step is null)
        {
            return NotFound();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var remainingIds = sequenceService.Remove(
            allSteps.Select(item => item.Id).ToArray(),
            stepId);
        var stepsById = allSteps.ToDictionary(item => item.Id);
        var remainingSteps = remainingIds.Select(id => stepsById[id]).ToList();
        var highestSequence = remainingSteps.Count == 0
            ? 0
            : remainingSteps.Max(item => item.SequenceNumber);
        if ((long)highestSequence + remainingSteps.Count + 1 > int.MaxValue)
        {
            await transaction.RollbackAsync(cancellationToken);
            TempData["ErrorMessage"] = "Nije moguće ukloniti korak zbog ograničenja redosleda.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        dbContext.ProductOperations.Remove(step);
        await dbContext.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < remainingSteps.Count; index++)
        {
            remainingSteps[index].SequenceNumber = highestSequence + index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        for (var index = 0; index < remainingSteps.Count; index++)
        {
            remainingSteps[index].SequenceNumber = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        TempData["SuccessMessage"] = "Korak je uklonjen iz rute.";
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Estimate(
        EstimateProductRouteViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await EditorView(
                viewModel.ProductId,
                cancellationToken,
                probeQuantity: viewModel.Quantity);
        }

        var route = await routeService.GetRouteAsync(viewModel.ProductId, cancellationToken);
        if (route is null)
        {
            return NotFound();
        }

        if (!route.IsReady)
        {
            return await EditorView(
                viewModel.ProductId,
                cancellationToken,
                viewModel.Quantity,
                "Procena nije dostupna dok ruta nije spremna za terminiranje.");
        }

        try
        {
            var duration = calculationService.EstimateDurationSeconds(
                route.Steps.Select(step =>
                    (step.SetupDurationMinutes, step.ProcessingSecondsPerUnit)),
                viewModel.Quantity);
            return await EditorView(
                viewModel.ProductId,
                cancellationToken,
                viewModel.Quantity,
                "Ukupno procenjeno vreme za izabranu količinu.",
                duration);
        }
        catch (OverflowException)
        {
            return await EditorView(
                viewModel.ProductId,
                cancellationToken,
                viewModel.Quantity,
                "Procena nije moguća jer je izračunata vrednost prevelika.");
        }
    }

    private async Task<IActionResult> MoveStepAsync(
        int productId,
        int stepId,
        int direction,
        CancellationToken cancellationToken)
    {
        var routeSteps = await dbContext.ProductOperations
            .Where(item => item.ProductId == productId)
            .OrderBy(item => item.SequenceNumber)
            .ToListAsync(cancellationToken);
        var step = routeSteps.SingleOrDefault(item => item.Id == stepId);
        if (step is null)
        {
            return NotFound();
        }

        var newOrder = sequenceService.Move(
            routeSteps.Select(item => item.Id).ToArray(),
            stepId,
            direction);
        if (newOrder.SequenceEqual(routeSteps.Select(item => item.Id)))
        {
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        var highestSequence = routeSteps.Max(item => item.SequenceNumber);
        if ((long)highestSequence + routeSteps.Count + 1 > int.MaxValue)
        {
            TempData["ErrorMessage"] = "Nije moguće promeniti redosled ove rute.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        for (var index = 0; index < routeSteps.Count; index++)
        {
            routeSteps[index].SequenceNumber = highestSequence + index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var stepsById = routeSteps.ToDictionary(item => item.Id);
        for (var index = 0; index < newOrder.Count; index++)
        {
            stepsById[newOrder[index]].SequenceNumber = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    private async Task<IActionResult> EditorView(
        int productId,
        CancellationToken cancellationToken,
        int probeQuantity = 100,
        string? estimationMessage = null,
        long? estimatedSeconds = null)
    {
        var editor = await routeService.GetEditorAsync(
            productId,
            probeQuantity,
            estimatedSeconds,
            estimationMessage,
            cancellationToken);
        return editor is null ? NotFound() : View(nameof(Edit), editor);
    }
}
