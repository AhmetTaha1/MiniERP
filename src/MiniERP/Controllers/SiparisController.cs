using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Entities;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Satis},{AppRoles.Muhasebe},{AppRoles.Yonetici}")]
public class SiparisController : Controller
{
    private const string YazmaRolleri = $"{AppRoles.Admin},{AppRoles.Satis}";

    private readonly AppDbContext _context;
    private readonly ISiparisService _siparisService;

    public SiparisController(AppDbContext context, ISiparisService siparisService)
    {
        _context = context;
        _siparisService = siparisService;
    }

    public async Task<IActionResult> Index(SiparisDurumu? durum, DateTime? baslangicTarihi, DateTime? bitisTarihi)
    {
        var query = _context.Siparisler
            .AsNoTracking()
            .Include(s => s.Musteri)
            .Include(s => s.Fatura)
            .AsQueryable();

        if (durum.HasValue)
        {
            query = query.Where(s => s.Durum == durum.Value);
        }

        if (baslangicTarihi.HasValue)
        {
            var baslangic = baslangicTarihi.Value.Date;
            query = query.Where(s => s.SiparisTarihi >= baslangic);
        }

        if (bitisTarihi.HasValue)
        {
            var bitis = bitisTarihi.Value.Date.AddDays(1);
            query = query.Where(s => s.SiparisTarihi < bitis);
        }

        ViewBag.Durum = durum;
        ViewBag.BaslangicTarihi = baslangicTarihi;
        ViewBag.BitisTarihi = bitisTarihi;

        return View(await query.OrderByDescending(s => s.Id).ToListAsync());
    }

    [Authorize(Roles = YazmaRolleri)]
    public async Task<IActionResult> Create()
    {
        await FormVerileriniYukleAsync();
        return View(new SiparisViewModel());
    }

    [HttpPost]
    [Authorize(Roles = YazmaRolleri)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SiparisViewModel model)
    {
        var sonuc = await _siparisService.OlusturAsync(model);
        if (sonuc.Basarili)
        {
            TempData["Success"] = $"Order #{sonuc.Id} created.";
            return RedirectToAction("Detay", new { id = sonuc.Id });
        }

        foreach (var hata in sonuc.Hatalar)
        {
            ModelState.AddModelError(string.Empty, hata);
        }

        await FormVerileriniYukleAsync();
        return View(model);
    }

    public async Task<IActionResult> Detay(int id)
    {
        var siparis = await _context.Siparisler
            .AsNoTracking()
            .Include(s => s.Musteri)
            .Include(s => s.Fatura)
            .Include(s => s.SiparisDetaylar)
                .ThenInclude(d => d.Urun)
                    .ThenInclude(u => u.Kategori)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (siparis == null) return NotFound();
        return View(siparis);
    }

    [Authorize(Roles = YazmaRolleri)]
    public async Task<IActionResult> Durum(int id)
    {
        var siparis = await SiparisMusteriyleGetirAsync(id);
        if (siparis == null) return NotFound();

        await DurumEkraniniHazirlaAsync(siparis);
        return View(siparis);
    }

    [HttpPost]
    [Authorize(Roles = YazmaRolleri)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Durum(int id, SiparisDurumu durum)
    {
        var sonuc = await _siparisService.DurumGuncelleAsync(id, durum);
        if (sonuc.Basarili)
        {
            return RedirectToAction("Detay", new { id });
        }

        var siparis = await SiparisMusteriyleGetirAsync(id);
        if (siparis == null) return NotFound();

        ModelState.AddModelError(string.Empty, sonuc.IlkHata);
        await DurumEkraniniHazirlaAsync(siparis);
        return View(siparis);
    }

    [HttpPost]
    [Authorize(Roles = YazmaRolleri)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var sonuc = await _siparisService.SilAsync(id);
        if (!sonuc.Basarili)
        {
            TempData["Error"] = sonuc.IlkHata;
        }

        return RedirectToAction("Index");
    }

    private Task<Siparis?> SiparisMusteriyleGetirAsync(int id) =>
        _context.Siparisler
            .AsNoTracking()
            .Include(s => s.Musteri)
            .FirstOrDefaultAsync(s => s.Id == id);

    private async Task FormVerileriniYukleAsync()
    {
        ViewBag.Musteriler = await _context.Musteriler
            .AsNoTracking()
            .OrderBy(m => m.Ad).ThenBy(m => m.Soyad)
            .ToListAsync();

        ViewBag.Urunler = await _context.Urunler
            .AsNoTracking()
            .Where(u => u.Aktif)
            .OrderBy(u => u.Ad)
            .ToListAsync();
    }

    private async Task DurumEkraniniHazirlaAsync(Siparis siparis)
    {
        var secenekler = new List<SiparisDurumu> { siparis.Durum };
        secenekler.AddRange(SiparisDurumKurallari.IzinliGecisler(siparis.Durum));

        var (tamamlanabilir, mesaj) = await _siparisService.TamamlanabilirMiAsync(siparis.Id);
        if (!tamamlanabilir)
        {
            secenekler.Remove(SiparisDurumu.Tamamlandi);
        }

        ViewBag.Durumlar = secenekler;
        ViewBag.TamamlanmaUyarisi = tamamlanabilir ? null : mesaj;
    }
}
