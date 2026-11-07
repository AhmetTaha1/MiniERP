using System.ComponentModel.DataAnnotations;

namespace MiniERP.Entities;

public class Kategori
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [MaxLength(100, ErrorMessage = "Maximum length is 100 characters")]
    public string Ad { get; set; } = string.Empty;

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public ICollection<Urun> Urunler { get; set; } = new List<Urun>();
}
