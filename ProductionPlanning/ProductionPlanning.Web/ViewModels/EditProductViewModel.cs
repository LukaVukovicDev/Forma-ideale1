using System.ComponentModel.DataAnnotations;

namespace ProductionPlanning.Web.ViewModels;

public class EditProductViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Šifra proizvoda je obavezna.")]
    [StringLength(40, ErrorMessage = "Šifra proizvoda može imati najviše 40 karaktera.")]
    [Display(Name = "Šifra proizvoda")]
    public string SifraProizvoda { get; set; } = string.Empty;

    [Required(ErrorMessage = "Naziv proizvoda je obavezan.")]
    [StringLength(200, ErrorMessage = "Naziv proizvoda može imati najviše 200 karaktera.")]
    [Display(Name = "Naziv")]
    public string Naziv { get; set; } = string.Empty;
}
