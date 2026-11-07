namespace MiniERP.Entities;

public class IslemLog
{
    public int Id { get; set; }
    public DateTime Tarih { get; set; } = DateTime.Now;
    public string Kullanici { get; set; } = string.Empty;
    public string Modul { get; set; } = string.Empty;
    public string Islem { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
}
