using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProductionPlanning.Web.ViewModels;

public class CreateOperationViewModel
{
    [Required(ErrorMessage = "Šifra operacije je obavezna.")]
    [StringLength(40, ErrorMessage = "Šifra može imati najviše 40 karaktera.")]
    [Display(Name = "Šifra operacije")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Naziv operacije je obavezan.")]
    [StringLength(200, ErrorMessage = "Naziv može imati najviše 200 karaktera.")]
    [Display(Name = "Naziv")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Opis")]
    public string? Description { get; set; }

    [Display(Name = "Aktivna")]
    public bool IsActive { get; set; } = true;
}

public class EditOperationViewModel : CreateOperationViewModel
{
    public int Id { get; set; }
}

public class OperationIndexItemViewModel
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public int CompatibleMachineCount { get; init; }
}

public class OperationDetailsViewModel
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public List<OperationMachineSelectionViewModel> Machines { get; init; } = [];
}

public class OperationMachineSelectionViewModel
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool IsSelected { get; set; }
}

public class SaveOperationCapabilitiesViewModel
{
    public int OperationDefinitionId { get; set; }

    public List<int> MachineIds { get; set; } = [];
}

public class AddProductRouteStepViewModel
{
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Izaberite aktivnu operaciju.")]
    [Display(Name = "Operacija")]
    public int OperationDefinitionId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Vreme pripreme ne može biti negativno.")]
    [Display(Name = "Vreme pripreme (minuti)")]
    public int SetupDurationMinutes { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vreme obrade mora biti veće od nule.")]
    [Display(Name = "Vreme obrade po komadu (sekunde)")]
    public int ProcessingSecondsPerUnit { get; set; }
}

public class EditProductRouteStepViewModel
{
    public int ProductId { get; set; }

    public int StepId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Izaberite postojeću aktivnu operaciju.")]
    public int OperationDefinitionId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Vreme pripreme ne može biti negativno.")]
    [Display(Name = "Vreme pripreme (minuti)")]
    public int SetupDurationMinutes { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vreme obrade mora biti veće od nule.")]
    [Display(Name = "Vreme obrade po komadu (sekunde)")]
    public int ProcessingSecondsPerUnit { get; set; }
}

public class ChangeProductRouteOperationViewModel
{
    public int ProductId { get; set; }

    public int StepId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Izaberite postojeću aktivnu operaciju.")]
    public int OperationDefinitionId { get; set; }
}

public class EstimateProductRouteViewModel
{
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Probna količina mora biti veća od nule.")]
    [Display(Name = "Probna količina (komada)")]
    public int Quantity { get; set; } = 100;
}

public class ProductRouteViewModel
{
    public int ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string ProductCode { get; init; } = string.Empty;

    public string ReadinessStatus { get; init; } = string.Empty;

    public bool IsReady { get; init; }

    public List<ProductRouteStepViewModel> Steps { get; init; } = [];
}

public class ProductRouteEditorViewModel
{
    public int ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string ProductCode { get; init; } = string.Empty;

    public string ReadinessStatus { get; init; } = string.Empty;

    public bool IsReady { get; init; }

    public List<ProductRouteStepViewModel> Steps { get; init; } = [];

    public List<SelectListItem> ActiveOperations { get; init; } = [];

    public int ProbeQuantity { get; init; } = 100;

    public long? EstimatedTotalSeconds { get; init; }

    public string? EstimationMessage { get; init; }
}

public class ProductRouteStepViewModel
{
    public int Id { get; init; }

    public int OperationDefinitionId { get; init; }

    public int SequenceNumber { get; init; }

    public string OperationCode { get; init; } = string.Empty;

    public string OperationName { get; init; } = string.Empty;

    public bool OperationIsActive { get; init; }

    public int SetupDurationMinutes { get; init; }

    public int ProcessingSecondsPerUnit { get; init; }

    public int ActiveCompatibleMachineCount { get; set; }

    public string CompatibleMachines { get; set; } = string.Empty;

    public long? EstimatedSeconds { get; set; }

    public List<SelectListItem> AvailableOperations { get; set; } = [];
}

public class RouteReadinessInput
{
    public int SequenceNumber { get; init; }

    public bool OperationIsActive { get; init; }

    public int SetupDurationMinutes { get; init; }

    public int ProcessingSecondsPerUnit { get; init; }

    public int ActiveCompatibleMachineCount { get; init; }
}
