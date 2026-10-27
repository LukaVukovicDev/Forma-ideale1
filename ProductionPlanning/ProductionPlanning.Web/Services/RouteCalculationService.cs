using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Services;

public class RouteCalculationService
{
    public string GetReadinessStatus(
        IReadOnlyCollection<RouteReadinessInput> steps)
    {
        if (steps.Count == 0)
        {
            return "Ruta nije definisana";
        }

        var ready = steps
            .OrderBy(step => step.SequenceNumber)
            .Select((step, index) =>
                step.SequenceNumber == index + 1
                && step.OperationIsActive
                && step.SetupDurationMinutes >= 0
                && step.ProcessingSecondsPerUnit > 0
                && step.ActiveCompatibleMachineCount > 0)
            .All(isStepReady => isStepReady);

        return ready ? "Spremna za terminiranje" : "Ruta nije spremna";
    }

    public long EstimateDurationSeconds(
        IEnumerable<(int SetupDurationMinutes, int ProcessingSecondsPerUnit)> steps,
        int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        long totalSeconds = 0;
        foreach (var step in steps)
        {
            if (step.SetupDurationMinutes < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(steps),
                    "Vreme pripreme ne može biti negativno.");
            }

            if (step.ProcessingSecondsPerUnit <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(steps),
                    "Norma obrade mora biti veća od nule.");
            }

            totalSeconds = checked(
                totalSeconds
                + checked((long)step.SetupDurationMinutes * 60)
                + checked((long)step.ProcessingSecondsPerUnit * quantity));
        }

        return totalSeconds;
    }
}
