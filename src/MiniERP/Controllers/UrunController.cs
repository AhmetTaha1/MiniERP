using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Entities;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Stok}")]
public class UrunController : Controller
{
    private readonly AppDbContext _context;
    private readonly IIslemLogService _logService;
    private readonly IKategoriService _kategoriService;

    public UrunController(AppDbContext context, IIslemLogService logService, IKategoriService kategoriService)
    {
        _context = context;
        _logService = logService;
        _kategoriService = kategoriService;
    }

    public async Task<IActionResult> Index()
    {
        var urunler = await _context.Urunler
            .AsNoTracking()
            .Include(u => u.Kategori)
            .Where(u => u.Aktif)
            .OrderByDescending(u => u.OlusturmaTarihi)
            .ThenByDescending(u => u.Id)
            .ToListAsync();
        return View(urunler);
    }

    public async Task<IActionResult> Create()
    {
        await KategoriListesiniYukleAsync();
        return View(new UrunFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UrunFormViewModel model)
    {
        model.Ad = (model.Ad ?? string.Empty).Trim();
        await AdBenzersizMiKontrolEtAsync(model.Ad, haricId: null);

        if (!ModelState.IsValid)
        {
            await KategoriListesiniYukleAsync();
            return View(model);
        }

        var kategori = await _kategoriService.BulVeyaEkleAsync(model.Kategori, model.YeniKategori);
        if (kategori == null)
        {
            ModelState.AddModelError(nameof(model.Kategori), "Select a category or enter a new category.");
            await KategoriListesiniYukleAsync();
            return View(model);
        }

        var urun = new Urun
        {
            Ad = model.Ad,
            Kategori = kategori,
            Fiyat = model.Fiyat,
            StokAdedi = model.StokAdedi,
            Aktif = true,
            OlusturmaTarihi = DateTime.Now
        };

        _context.Urunler.Add(urun);
        await _context.SaveChangesAsync();
        await _logService.AddAsync(LogModul.Urun, LogIslem.Olusturma, $"{urun.Ad} product was added.");
        TempData["Success"] = "Product added.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var urun = await _context.Urunler
            .AsNoTracking()
            .Include(u => u.Kategori)
            .FirstOrDefaultAsync(u => u.Id == id && u.Aktif);
        if (urun == null) return NotFound();

        await KategoriListesiniYukleAsync();
        return View(new UrunFormViewModel
        {
            Id = urun.Id,
            Ad = urun.Ad,
            Kategori = urun.Kategori.Ad,
            Fiyat = urun.Fiyat,
            StokAdedi = urun.StokAdedi,
            OrijinalStokAdedi = urun.StokAdedi,
            OlusturmaTarihi = urun.OlusturmaTarihi
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UrunFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        model.Ad = (model.Ad ?? string.Empty).Trim();
        await AdBenzersizMiKontrolEtAsync(model.Ad, haricId: id);

        var urun = await _context.Urunler.FirstOrDefaultAsync(u => u.Id == id && u.Aktif);
        if (urun == null) return NotFound();

        Kategori? kategori = null;
        if (ModelState.IsValid)
        {
            kategori = await _kategoriService.BulVeyaEkleAsync(model.Kategori, model.YeniKategori);
            if (kategori == null)
            {
                ModelState.AddModelError(nameof(model.Kategori), "Select a category or enter a new category.");
            }
        }

        if (!ModelState.IsValid || kategori == null)
        {
            await KategoriListesiniYukleAsync();
            return View(model);
        }

        // If an order changed the stock after the form was opened, the save fails instead of
        // silently overwriting the new stock value (lost update).
        if (model.OrijinalStokAdedi.HasValue)
        {
            _context.Entry(urun).Property(u => u.StokAdedi).OriginalValue = model.OrijinalStokAdedi.Value;
        }

        urun.Ad = model.Ad;
        urun.Kategori = kategori;
        urun.Fiyat = model.Fiyat;
        urun.StokAdedi = model.StokAdedi;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Stock for this product changed (e.g. an order was placed) while you were editing. Reload the page and try again.");
            await KategoriListesiniYukleAsync();
            return View(model);
        }

        await _logService.AddAsync(LogModul.Urun, LogIslem.Guncelleme, $"{urun.Ad} product was updated.");
        TempData["Success"] = "Product updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var urun = await _context.Urunler.FirstOrDefaultAsync(u => u.Id == id && u.Aktif);
        if (urun == null) return NotFound();

        var aktifSiparisteVarMi = await _context.SiparisDetaylar
            .AnyAsync(d => d.UrunId == id && SiparisDurumKurallari.AktifDurumlar.Contains(d.Siparis.Durum));

        if (aktifSiparisteVarMi)
        {
            TempData["Error"] = "Products used in active orders cannot be deleted. You can deactivate the product after the order is completed or canceled.";
            return RedirectToAction("Index");
        }

        urun.Aktif = false;
        await _context.SaveChangesAsync();
        await _logService.AddAsync(LogModul.Urun, LogIslem.PasifeAlma, $"{urun.Ad} product was removed from the list.");
        TempData["Success"] = "Product removed from the list. Historical order records are preserved.";
        return RedirectToAction("Index");
    }

    private async Task AdBenzersizMiKontrolEtAsync(string ad, int? haricId)
    {
        var normalized = ad.ToLower();
        if (await _context.Urunler.AnyAsync(u => u.Aktif && u.Ad.ToLower() == normalized && u.Id != haricId))
        {
            ModelState.AddModelError(nameof(UrunFormViewModel.Ad), "Another active product is already registered with this name.");
        }
    }

    private async Task KategoriListesiniYukleAsync()
    {
        ViewBag.Kategoriler = await _context.Kategoriler
            .AsNoTracking()
            .OrderBy(k => k.Ad)
            .Select(k => k.Ad)
            .ToListAsync();
    }
}
