namespace ProductionPlanning.Web.Services;

public class RouteSequenceService
{
    public IReadOnlyList<int> Remove(
        IReadOnlyList<int> stepIdsInOrder,
        int stepId)
    {
        if (!stepIdsInOrder.Contains(stepId))
        {
            throw new ArgumentException("Korak nije deo prosleđene rute.", nameof(stepId));
        }

        return stepIdsInOrder.Where(candidate => candidate != stepId).ToArray();
    }

    public IReadOnlyList<int> Move(
        IReadOnlyList<int> stepIdsInOrder,
        int stepId,
        int direction)
    {
        if (direction is not (-1 or 1))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        var index = -1;
        for (var candidate = 0; candidate < stepIdsInOrder.Count; candidate++)
        {
            if (stepIdsInOrder[candidate] == stepId)
            {
                index = candidate;
                break;
            }
        }

        if (index < 0)
        {
            throw new ArgumentException("Korak nije deo prosleđene rute.", nameof(stepId));
        }

        var target = index + direction;
        var reordered = stepIdsInOrder.ToArray();
        if (target >= 0 && target < reordered.Length)
        {
            (reordered[index], reordered[target]) = (reordered[target], reordered[index]);
        }

        return reordered;
    }
}
