namespace MiniERP.Models;

public class RaporFiltresi
{
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public string? Kategori { get; set; }
}

public class RaporViewModel
{
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public string? Kategori { get; set; }
    public List<string> Kategoriler { get; set; } = new();
    public decimal ToplamGelir { get; set; }
    public int BekleyenSiparisSayisi { get; set; }
    public int KritikStokSayisi { get; set; }
    public int OdenmemisFaturaSayisi { get; set; }
    public decimal OdenmemisFaturaTutari { get; set; }
    public List<RaporAyItem> AylikGelirler { get; set; } = new();
    public List<RaporUrunItem> EnCokSatanUrunler { get; set; } = new();
    public List<RaporKategoriItem> KategoriUrunRaporu { get; set; } = new();
    public List<RaporMusteriItem> MusteriSatisRaporu { get; set; } = new();
    public List<RaporStokItem> KritikStoklar { get; set; } = new();

    public string FiltreMetni
    {
        get
        {
            var baslangic = BaslangicTarihi?.ToString("dd.MM.yyyy") ?? "All time";
            var bitis = BitisTarihi?.ToString("dd.MM.yyyy") ?? "To date";
            var kategori = string.IsNullOrWhiteSpace(Kategori) ? "All categories" : Kategori;
            return $"Date range: {baslangic} - {bitis} | Category: {kategori}";
        }
    }
}

public class RaporAyItem
{
    public string Ay { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
}

public class RaporUrunItem
{
    public string UrunAdi { get; set; } = string.Empty;
    public int Miktar { get; set; }
    public decimal Tutar { get; set; }
}

public class RaporStokItem
{
    public string UrunAdi { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public int StokAdedi { get; set; }
}

public class RaporKategoriItem
{
    public string Kategori { get; set; } = string.Empty;
    public string UrunAdi { get; set; } = string.Empty;
    public int SatilanAdet { get; set; }
    public decimal Tutar { get; set; }
    public int MevcutStok { get; set; }
}

public class RaporMusteriItem
{
    public string Musteri { get; set; } = string.Empty;
    public int SiparisSayisi { get; set; }
    public decimal ToplamTutar { get; set; }
}
