using System.ComponentModel.DataAnnotations;

namespace MiniERP.Models;

public class UrunFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [MaxLength(100, ErrorMessage = "Maximum length is 100 characters")]
    public string Ad { get; set; } = string.Empty;

    /// <summary>Name of an existing category selected from the list.</summary>
    public string? Kategori { get; set; }

    /// <summary>Optional new category name; takes precedence over <see cref="Kategori"/>.</summary>
    [MaxLength(100, ErrorMessage = "Maximum length is 100 characters")]
    public string? YeniKategori { get; set; }

    [Range(0.01, 999999, ErrorMessage = "Enter a valid price")]
    public decimal Fiyat { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    public int StokAdedi { get; set; }

    /// <summary>Stock value when the edit form was opened; used for optimistic concurrency.</summary>
    public int? OrijinalStokAdedi { get; set; }

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
