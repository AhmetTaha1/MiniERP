using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Entities;
using MiniERP.Models;

namespace MiniERP.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> OlusturAsync();
}

/// <summary>Builds the dashboard with database-side aggregation (no full-table loads).</summary>
public class DashboardService : IDashboardService
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> OlusturAsync()
    {
        var faturalar = _context.Faturalar.AsNoTracking();
        var urunler = _context.Urunler.AsNoTracking().Where(u => u.Aktif);
        var siparisler = _context.Siparisler.AsNoTracking();
        var esik = StokKurallari.KritikStokEsigi;
        var bugun = DateTime.Today;
        var yarin = bugun.AddDays(1);

        var aylikGelirler = (await faturalar
                .Where(f => f.OdenmeDurumu)
                .GroupBy(f => new { f.FaturaTarihi.Year, f.FaturaTarihi.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Toplam = g.Sum(f => f.ToplamTutar) })
                .ToListAsync())
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .TakeLast(6)
            .Select(x => new GrafikVerisi
            {
                Etiket = new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy", EnUs),
                Deger = x.Toplam
            })
            .ToList();

        var kategoriDagilimi = await urunler
            .GroupBy(u => u.Kategori.Ad)
            .Select(g => new GrafikVerisi { Etiket = g.Key, Deger = g.Count() })
            .OrderByDescending(x => x.Deger)
            .Take(6)
            .ToListAsync();

        var durumDagilimi = (await siparisler
                .GroupBy(s => s.Durum)
                .Select(g => new { Durum = g.Key, Adet = g.Count() })
                .ToListAsync())
            .Select(x => new GrafikVerisi { Etiket = DisplayText.OrderStatus(x.Durum), Deger = x.Adet })
            .ToList();

        var kritikStokSayisi = await urunler.CountAsync(u => u.StokAdedi <= esik);
        var guvenliStokSayisi = await urunler.CountAsync(u => u.StokAdedi > esik);

        return new DashboardViewModel
        {
            ToplamGelir = await faturalar.Where(f => f.OdenmeDurumu).SumAsync(f => f.ToplamTutar),
            MusteriSayisi = await _context.Musteriler.CountAsync(),
            BekleyenSiparisSayisi = await siparisler.CountAsync(s => s.Durum == SiparisDurumu.Bekliyor),
            KritikStokSayisi = kritikStokSayisi,
            OdenmemisFaturaSayisi = await faturalar.CountAsync(f => !f.OdenmeDurumu),
            OdenmemisFaturaTutari = await faturalar.Where(f => !f.OdenmeDurumu).SumAsync(f => f.ToplamTutar),
            BugunkuSiparisSayisi = await siparisler.CountAsync(s => s.SiparisTarihi >= bugun && s.SiparisTarihi < yarin),
            AylikGelirler = aylikGelirler,
            KategoriDagilimi = kategoriDagilimi,
            SiparisDurumDagilimi = durumDagilimi,
            StokSeviyesiDagilimi =
            [
                new GrafikVerisi { Etiket = "Safe Stock", Deger = guvenliStokSayisi },
                new GrafikVerisi { Etiket = "Critical Stock", Deger = kritikStokSayisi }
            ],
            SonSiparisler = await siparisler
                .OrderByDescending(s => s.SiparisTarihi)
                .Take(5)
                .Select(s => new DashboardSiparisItem
                {
                    Id = s.Id,
                    Musteri = string.IsNullOrEmpty(s.Musteri.Soyad) ? s.Musteri.Ad : s.Musteri.Ad + " " + s.Musteri.Soyad,
                    Tarih = s.SiparisTarihi,
                    ToplamTutar = s.ToplamTutar,
                    Durum = s.Durum
                })
                .ToListAsync(),
            KritikStoklar = await urunler
                .Where(u => u.StokAdedi <= esik)
                .OrderBy(u => u.StokAdedi).ThenBy(u => u.Ad)
                .Take(5)
                .Select(u => new DashboardStokItem
                {
                    Id = u.Id,
                    UrunAdi = u.Ad,
                    Kategori = u.Kategori.Ad,
                    StokAdedi = u.StokAdedi
                })
                .ToListAsync(),
            OdenmemisFaturalar = await faturalar
                .Where(f => !f.OdenmeDurumu)
                .OrderByDescending(f => f.FaturaTarihi)
                .Take(5)
                .Select(f => new DashboardFaturaItem
                {
                    Id = f.Id,
                    FaturaNo = f.FaturaNo,
                    Musteri = string.IsNullOrEmpty(f.Siparis.Musteri.Soyad)
                        ? f.Siparis.Musteri.Ad
                        : f.Siparis.Musteri.Ad + " " + f.Siparis.Musteri.Soyad,
                    ToplamTutar = f.ToplamTutar
                })
                .ToListAsync()
        };
    }
}
