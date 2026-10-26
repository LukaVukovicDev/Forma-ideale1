using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProductionPlanning.Web.ViewModels;

public class CreateProductionRecordViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Izaberite plan proizvodnje.")]
    [Display(Name = "Plan proizvodnje")]
    public int PlannedProductionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Proizvedena količina mora biti veća od nule.")]
    [Display(Name = "Proizvedena količina")]
    public int Quantity { get; set; }

    public List<SelectListItem> PlannedProductions { get; set; } = [];
}
