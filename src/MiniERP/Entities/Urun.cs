using System.ComponentModel.DataAnnotations;

namespace MiniERP.Entities;

public class Urun
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    [MaxLength(100, ErrorMessage = "Maximum length is 100 characters")]
    public string Ad { get; set; } = string.Empty;

    public int KategoriId { get; set; }
    public Kategori Kategori { get; set; } = null!;

    [Range(0.01, 999999, ErrorMessage = "Enter a valid price")]
    public decimal Fiyat { get; set; }

    /// <summary>
    /// Configured as a concurrency token: two requests that read the same stock value
    /// cannot both decrement it — the second save fails instead of overselling.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
    public int StokAdedi { get; set; }

    public bool Aktif { get; set; } = true;

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
