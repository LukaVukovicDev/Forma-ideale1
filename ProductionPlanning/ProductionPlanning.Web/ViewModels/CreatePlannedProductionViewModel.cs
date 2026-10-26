using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProductionPlanning.Web.ViewModels;

public class CreatePlannedProductionViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Izaberite proizvod.")]
    [Display(Name = "Proizvod")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Izaberite aktivnu mašinu.")]
    [Display(Name = "Mašina")]
    public int MachineId { get; set; }

    [Required(ErrorMessage = "Datum planirane proizvodnje je obavezan.")]
    [Display(Name = "Datum planirane proizvodnje")]
    [DataType(DataType.Date)]
    public DateOnly? PlannedDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Planirana količina mora biti veća od nule.")]
    [Display(Name = "Planirana količina")]
    public int PlannedQuantity { get; set; }

    public List<SelectListItem> Products { get; set; } = [];

    public List<SelectListItem> Machines { get; set; } = [];
}
