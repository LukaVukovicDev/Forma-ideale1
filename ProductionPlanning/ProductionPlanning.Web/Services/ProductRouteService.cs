using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Services;

public class ProductRouteService(
    ApplicationDbContext dbContext,
    RouteCalculationService calculationService)
{
    public async Task<ProductRouteViewModel?> GetRouteAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == productId)
            .Select(item => new
            {
                item.Id,
                item.Naziv,
                item.SifraProizvoda
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return null;
        }

        var steps = await dbContext.ProductOperations
            .AsNoTracking()
            .Where(step => step.ProductId == productId)
            .OrderBy(step => step.SequenceNumber)
            .Select(step => new ProductRouteStepViewModel
            {
                Id = step.Id,
                OperationDefinitionId = step.OperationDefinitionId,
                SequenceNumber = step.SequenceNumber,
                OperationCode = step.OperationDefinition.Code,
                OperationName = step.OperationDefinition.Name,
                OperationIsActive = step.OperationDefinition.IsActive,
                SetupDurationMinutes = step.SetupDurationMinutes,
                ProcessingSecondsPerUnit = step.ProcessingSecondsPerUnit
            })
            .ToListAsync(cancellationToken);

        await AddMachineCompatibilityAsync(steps, cancellationToken);

        var readinessInputs = steps
            .Select(step => new RouteReadinessInput
            {
                SequenceNumber = step.SequenceNumber,
                OperationIsActive = step.OperationIsActive,
                SetupDurationMinutes = step.SetupDurationMinutes,
                ProcessingSecondsPerUnit = step.ProcessingSecondsPerUnit,
                ActiveCompatibleMachineCount = step.ActiveCompatibleMachineCount
            })
            .ToArray();
        var status = calculationService.GetReadinessStatus(readinessInputs);
        foreach (var (step, index) in steps.Select((step, index) => (step, index)))
        {
            step.ReadinessIssues = calculationService.GetStepReadinessIssues(
                readinessInputs[index],
                index + 1);
        }

        return new ProductRouteViewModel
        {
            ProductId = product.Id,
            ProductName = product.Naziv,
            ProductCode = product.SifraProizvoda,
            ReadinessStatus = status,
            IsReady = status == "Spremna za terminiranje",
            ReadinessReasons = calculationService.GetReadinessIssues(readinessInputs),
            Steps = steps
        };
    }

    public async Task<ProductRouteEditorViewModel?> GetEditorAsync(
        int productId,
        int probeQuantity = 100,
        long? estimatedTotalSeconds = null,
        string? estimationMessage = null,
        CancellationToken cancellationToken = default)
    {
        var route = await GetRouteAsync(productId, cancellationToken);
        if (route is null)
        {
            return null;
        }

        foreach (var step in route.Steps)
        {
            try
            {
                step.EstimatedSeconds = probeQuantity > 0
                    ? calculationService.EstimateDurationSeconds(
                        [(step.SetupDurationMinutes, step.ProcessingSecondsPerUnit)],
                        probeQuantity)
                    : null;
            }
            catch (OverflowException)
            {
                step.EstimatedSeconds = null;
            }
        }

        var operations = await dbContext.OperationDefinitions
            .AsNoTracking()
            .Where(operation => operation.IsActive)
            .OrderBy(operation => operation.Name)
            .Select(operation => new SelectListItem(
                $"{operation.Code} — {operation.Name}",
                operation.Id.ToString()))
            .ToListAsync(cancellationToken);

        foreach (var step in route.Steps)
        {
            step.AvailableOperations = operations
                .Select(operation => new SelectListItem(
                    operation.Text,
                    operation.Value,
                    operation.Value == step.OperationDefinitionId.ToString()))
                .ToList();

            if (!step.OperationIsActive)
            {
                step.AvailableOperations.Insert(
                    0,
                    new SelectListItem(
                        $"{step.OperationCode} — {step.OperationName} (neaktivna)",
                        step.OperationDefinitionId.ToString(),
                        selected: true));
            }
        }

        return new ProductRouteEditorViewModel
        {
            ProductId = route.ProductId,
            ProductName = route.ProductName,
            ProductCode = route.ProductCode,
            ReadinessStatus = route.ReadinessStatus,
            IsReady = route.IsReady,
            ReadinessReasons = route.ReadinessReasons,
            Steps = route.Steps,
            ActiveOperations = operations,
            ProbeQuantity = probeQuantity,
            EstimatedTotalSeconds = estimatedTotalSeconds,
            EstimationMessage = estimationMessage
        };
    }

    private async Task AddMachineCompatibilityAsync(
        List<ProductRouteStepViewModel> steps,
        CancellationToken cancellationToken)
    {
        if (steps.Count == 0)
        {
            return;
        }

        var operationIds = steps
            .Select(step => step.OperationDefinitionId)
            .Distinct()
            .ToArray();

        var compatibleMachines = await dbContext.MachineOperationCapabilities
            .AsNoTracking()
            .Where(capability => operationIds.Contains(capability.OperationDefinitionId))
            .Select(capability => new
            {
                capability.OperationDefinitionId,
                capability.Machine.Id,
                capability.Machine.SifraMasine,
                capability.Machine.Naziv,
                capability.Machine.IsActive
            })
            .OrderBy(machine => machine.Naziv)
            .ToListAsync(cancellationToken);

        foreach (var step in steps)
        {
            var machines = compatibleMachines
                .Where(machine => machine.OperationDefinitionId == step.OperationDefinitionId)
                .ToArray();
            step.ActiveCompatibleMachineCount = machines.Count(machine => machine.IsActive);
            step.CompatibleMachines = machines.Length == 0
                ? "Nema dodeljenih mašina"
                : string.Join(", ", machines.Select(machine =>
                    $"{machine.SifraMasine} — {machine.Naziv}{(machine.IsActive ? string.Empty : " (neaktivna)")}"));
        }
    }
}
