namespace MiniERP.Entities;

public class SiparisDetay
{
    public int Id { get; set; }
    public int SiparisId { get; set; }
    public Siparis Siparis { get; set; } = null!;
    public int UrunId { get; set; }
    public Urun Urun { get; set; } = null!;
    public int Miktar { get; set; }
    public decimal BirimFiyat { get; set; }
}