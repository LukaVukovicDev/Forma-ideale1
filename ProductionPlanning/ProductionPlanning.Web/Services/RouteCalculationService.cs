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

        return GetReadinessIssues(steps).Count == 0
            ? "Spremna za terminiranje"
            : "Ruta nije spremna";
    }

    public List<string> GetReadinessIssues(
        IReadOnlyCollection<RouteReadinessInput> steps)
    {
        var issues = new List<string>();
        var orderedSteps = steps.OrderBy(step => step.SequenceNumber).ToArray();

        for (var index = 0; index < orderedSteps.Length; index++)
        {
            var step = orderedSteps[index];
            var stepIssues = GetStepReadinessIssues(step, index + 1);
            issues.AddRange(stepIssues.Select(issue =>
                $"Korak {step.SequenceNumber}: {issue}"));
        }

        return issues;
    }

    public List<string> GetStepReadinessIssues(
        RouteReadinessInput step,
        int expectedSequence)
    {
        var issues = new List<string>();
        if (step.SequenceNumber != expectedSequence)
        {
            issues.Add($"redosled nije kontinuiran (očekivan redni broj {expectedSequence})");
        }

        if (!step.OperationIsActive)
        {
            issues.Add("tehnološka operacija nije aktivna");
        }

        if (step.SetupDurationMinutes < 0)
        {
            issues.Add("vreme pripreme ne može biti negativno");
        }

        if (step.ProcessingSecondsPerUnit <= 0)
        {
            issues.Add("vreme obrade po komadu mora biti veće od nule");
        }

        if (step.ActiveCompatibleMachineCount <= 0)
        {
            issues.Add("nema aktivne kompatibilne mašine ili radnog centra");
        }

        return issues;
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
