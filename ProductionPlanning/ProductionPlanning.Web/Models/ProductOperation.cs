namespace ProductionPlanning.Web.Models;

public class ProductOperation
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int OperationDefinitionId { get; set; }

    public int SequenceNumber { get; set; }

    public int SetupDurationMinutes { get; set; }

    public int ProcessingSecondsPerUnit { get; set; }

    public Product Product { get; set; } = null!;

    public OperationDefinition OperationDefinition { get; set; } = null!;
}
