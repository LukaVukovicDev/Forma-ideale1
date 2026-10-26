using System.ComponentModel.DataAnnotations;

namespace ProductionPlanning.Web.ViewModels;

public class CreateMachineViewModel
{
    [Required(ErrorMessage = "Šifra mašine je obavezna.")]
    [StringLength(40)]
    [Display(Name = "Šifra mašine")]
    public string SifraMasine { get; set; } = string.Empty;

    [Required(ErrorMessage = "Naziv mašine je obavezan.")]
    [StringLength(200)]
    [Display(Name = "Naziv mašine")]
    public string Naziv { get; set; } = string.Empty;

    [Display(Name = "Aktivna za nove planove")]
    public bool IsActive { get; set; } = true;
}
