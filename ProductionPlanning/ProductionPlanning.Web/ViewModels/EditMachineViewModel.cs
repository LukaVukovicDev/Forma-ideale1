using System.ComponentModel.DataAnnotations;

namespace ProductionPlanning.Web.ViewModels;

public class EditMachineViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Šifra mašine je obavezna.")]
    [StringLength(40, ErrorMessage = "Šifra mašine može imati najviše 40 karaktera.")]
    [Display(Name = "Šifra mašine")]
    public string SifraMasine { get; set; } = string.Empty;

    [Required(ErrorMessage = "Naziv mašine je obavezan.")]
    [StringLength(200, ErrorMessage = "Naziv mašine može imati najviše 200 karaktera.")]
    [Display(Name = "Naziv mašine")]
    public string Naziv { get; set; } = string.Empty;

    [Display(Name = "Aktivna za nove planove")]
    public bool IsActive { get; set; }
}
