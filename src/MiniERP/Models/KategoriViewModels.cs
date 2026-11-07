namespace MiniERP.Models;

public class KategoriListItemViewModel
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int AktifUrunSayisi { get; set; }
    public int ToplamUrunSayisi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    /// <summary>Only categories that were never used by any product can be deleted (FK integrity).</summary>
    public bool Silinebilir => ToplamUrunSayisi == 0;
}
