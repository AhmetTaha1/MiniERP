using MiniERP.Entities;

namespace MiniERP.Models;

public class DashboardViewModel
{
    public decimal ToplamGelir { get; set; }
    public int MusteriSayisi { get; set; }
    public int BekleyenSiparisSayisi { get; set; }
    public int KritikStokSayisi { get; set; }
    public int OdenmemisFaturaSayisi { get; set; }
    public decimal OdenmemisFaturaTutari { get; set; }
    public int BugunkuSiparisSayisi { get; set; }
    public List<GrafikVerisi> AylikGelirler { get; set; } = new();
    public List<GrafikVerisi> KategoriDagilimi { get; set; } = new();
    public List<GrafikVerisi> SiparisDurumDagilimi { get; set; } = new();
    public List<GrafikVerisi> StokSeviyesiDagilimi { get; set; } = new();
    public List<DashboardSiparisItem> SonSiparisler { get; set; } = new();
    public List<DashboardStokItem> KritikStoklar { get; set; } = new();
    public List<DashboardFaturaItem> OdenmemisFaturalar { get; set; } = new();
}

public class GrafikVerisi
{
    public string Etiket { get; set; } = string.Empty;
    public decimal Deger { get; set; }
}

public class DashboardSiparisItem
{
    public int Id { get; set; }
    public string Musteri { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public decimal ToplamTutar { get; set; }
    public SiparisDurumu Durum { get; set; }
}

public class DashboardStokItem
{
    public int Id { get; set; }
    public string UrunAdi { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public int StokAdedi { get; set; }
}

public class DashboardFaturaItem
{
    public int Id { get; set; }
    public string FaturaNo { get; set; } = string.Empty;
    public string Musteri { get; set; } = string.Empty;
    public decimal ToplamTutar { get; set; }
}
