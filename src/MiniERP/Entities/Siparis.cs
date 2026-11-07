namespace MiniERP.Entities;

public class Siparis
{
    public int Id { get; set; }
    public int MusteriId { get; set; }
    public Musteri Musteri { get; set; } = null!;
    public DateTime SiparisTarihi { get; set; } = DateTime.Now;
    public decimal ToplamTutar { get; set; }
    public SiparisDurumu Durum { get; set; } = SiparisDurumu.Bekliyor;
    public Fatura? Fatura { get; set; }
    public ICollection<SiparisDetay> SiparisDetaylar { get; set; } = new List<SiparisDetay>();
}
