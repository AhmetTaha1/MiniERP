using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Entities;
using MiniERP.Models;

namespace MiniERP.Services;

public interface IRaporService
{
    Task<RaporViewModel> OlusturAsync(RaporFiltresi filtre);
}

/// <summary>
/// Management report. Revenue figures come from invoices (filtered by invoice date);
/// sales figures come from order lines (filtered by order date) and exclude canceled orders.
/// </summary>
public class RaporService : IRaporService
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");
    private readonly AppDbContext _context;

    public RaporService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RaporViewModel> OlusturAsync(RaporFiltresi filtre)
    {
        var kategori = string.IsNullOrWhiteSpace(filtre.Kategori) ? null : filtre.Kategori.Trim();
        var baslangic = filtre.BaslangicTarihi?.Date;
        var bitisHaric = filtre.BitisTarihi?.Date.AddDays(1);
        var esik = StokKurallari.KritikStokEsigi;

        var faturalar = _context.Faturalar.AsNoTracking();
        if (baslangic.HasValue) faturalar = faturalar.Where(f => f.FaturaTarihi >= baslangic.Value);
        if (bitisHaric.HasValue) faturalar = faturalar.Where(f => f.FaturaTarihi < bitisHaric.Value);

        var satislar = _context.SiparisDetaylar
            .AsNoTracking()
            .Where(d => d.Siparis.Durum != SiparisDurumu.Iptal);
        if (baslangic.HasValue) satislar = satislar.Where(d => d.Siparis.SiparisTarihi >= baslangic.Value);
        if (bitisHaric.HasValue) satislar = satislar.Where(d => d.Siparis.SiparisTarihi < bitisHaric.Value);
        if (kategori != null) satislar = satislar.Where(d => d.Urun.Kategori.Ad == kategori);

        var kritikStokSorgu = _context.Urunler.AsNoTracking().Where(u => u.Aktif && u.StokAdedi <= esik);
        if (kategori != null) kritikStokSorgu = kritikStokSorgu.Where(u => u.Kategori.Ad == kategori);

        var kritikStoklar = await kritikStokSorgu
            .OrderBy(u => u.StokAdedi).ThenBy(u => u.Ad)
            .Select(u => new RaporStokItem { UrunAdi = u.Ad, Kategori = u.Kategori.Ad, StokAdedi = u.StokAdedi })
            .ToListAsync();

        var aylikGelirler = (await faturalar
                .Where(f => f.OdenmeDurumu)
                .GroupBy(f => new { f.FaturaTarihi.Year, f.FaturaTarihi.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Toplam = g.Sum(f => f.ToplamTutar) })
                .ToListAsync())
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .TakeLast(6)
            .Select(x => new RaporAyItem
            {
                Ay = new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy", EnUs),
                Tutar = x.Toplam
            })
            .ToList();

        var urunSatislari = await satislar
            .GroupBy(d => new { d.UrunId, d.Urun.Ad, KategoriAd = d.Urun.Kategori.Ad, d.Urun.StokAdedi })
            .Select(g => new RaporKategoriItem
            {
                Kategori = g.Key.KategoriAd,
                UrunAdi = g.Key.Ad,
                SatilanAdet = g.Sum(d => d.Miktar),
                Tutar = g.Sum(d => d.Miktar * d.BirimFiyat),
                MevcutStok = g.Key.StokAdedi
            })
            .ToListAsync();

        var musteriSatislari = await satislar
            .GroupBy(d => new { d.Siparis.MusteriId, d.Siparis.Musteri.Ad, d.Siparis.Musteri.Soyad })
            .Select(g => new
            {
                g.Key.Ad,
                g.Key.Soyad,
                SiparisSayisi = g.Select(d => d.SiparisId).Distinct().Count(),
                ToplamTutar = g.Sum(d => d.Miktar * d.BirimFiyat)
            })
            .ToListAsync();

        return new RaporViewModel
        {
            BaslangicTarihi = filtre.BaslangicTarihi,
            BitisTarihi = filtre.BitisTarihi,
            Kategori = kategori,
            Kategoriler = await _context.Kategoriler.AsNoTracking().OrderBy(k => k.Ad).Select(k => k.Ad).ToListAsync(),
            ToplamGelir = await faturalar.Where(f => f.OdenmeDurumu).SumAsync(f => f.ToplamTutar),
            BekleyenSiparisSayisi = await _context.Siparisler.CountAsync(s => s.Durum == SiparisDurumu.Bekliyor),
            KritikStokSayisi = kritikStoklar.Count,
            OdenmemisFaturaSayisi = await faturalar.CountAsync(f => !f.OdenmeDurumu),
            OdenmemisFaturaTutari = await faturalar.Where(f => !f.OdenmeDurumu).SumAsync(f => f.ToplamTutar),
            AylikGelirler = aylikGelirler,
            KritikStoklar = kritikStoklar,
            EnCokSatanUrunler = urunSatislari
                .OrderByDescending(u => u.SatilanAdet)
                .Take(5)
                .Select(u => new RaporUrunItem { UrunAdi = u.UrunAdi, Miktar = u.SatilanAdet, Tutar = u.Tutar })
                .ToList(),
            KategoriUrunRaporu = urunSatislari
                .OrderBy(r => r.Kategori)
                .ThenByDescending(r => r.SatilanAdet)
                .ToList(),
            MusteriSatisRaporu = musteriSatislari
                .Select(m => new RaporMusteriItem
                {
                    Musteri = string.IsNullOrWhiteSpace(m.Soyad) ? m.Ad : $"{m.Ad} {m.Soyad}",
                    SiparisSayisi = m.SiparisSayisi,
                    ToplamTutar = m.ToplamTutar
                })
                .OrderByDescending(r => r.SiparisSayisi)
                .ThenByDescending(r => r.ToplamTutar)
                .ToList()
        };
    }
}
