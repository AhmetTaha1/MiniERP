using System.ComponentModel.DataAnnotations;

namespace MiniERP.Entities;

public class Musteri
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Customer/company name is required")]
    [Display(Name = "Customer / Company Name")]
    [MaxLength(50, ErrorMessage = "Maximum length is 50 characters")]
    public string Ad { get; set; } = string.Empty;

    [Display(Name = "Contact Last Name")]
    [MaxLength(50, ErrorMessage = "Maximum length is 50 characters")]
    public string? Soyad { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^[+0-9 ()\-]{7,20}$", ErrorMessage = "Enter a valid phone number")]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string Telefon { get; set; } = string.Empty;

    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string Adres { get; set; } = string.Empty;

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public ICollection<Siparis> Siparisler { get; set; } = new List<Siparis>();

    public string AdSoyad => string.IsNullOrWhiteSpace(Soyad) ? Ad : $"{Ad} {Soyad}";
}
