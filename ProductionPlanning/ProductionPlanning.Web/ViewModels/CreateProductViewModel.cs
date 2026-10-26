using System.ComponentModel.DataAnnotations;

namespace ProductionPlanning.Web.ViewModels;

public class CreateProductViewModel
{
    [Required(ErrorMessage = "Šifra proizvoda je obavezna.")]
    [StringLength(40)]
    [Display(Name = "Šifra proizvoda")]
    public string SifraProizvoda { get; set; } = string.Empty;

    [Required(ErrorMessage = "Naziv proizvoda je obavezan.")]
    [StringLength(200)]
    [Display(Name = "Naziv proizvoda")]
    public string Naziv { get; set; } = string.Empty;
}