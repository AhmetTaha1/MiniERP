using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Entities;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Stok}")]
public class KategoriController : Controller
{
    private readonly AppDbContext _context;
    private readonly IIslemLogService _logService;

    public KategoriController(AppDbContext context, IIslemLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _context.Kategoriler
            .AsNoTracking()
            .OrderBy(k => k.Ad)
            .Select(k => new KategoriListItemViewModel
            {
                Id = k.Id,
                Ad = k.Ad,
                OlusturmaTarihi = k.OlusturmaTarihi,
                AktifUrunSayisi = k.Urunler.Count(u => u.Aktif),
                ToplamUrunSayisi = k.Urunler.Count()
            })
            .ToListAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? ad)
    {
        ad = ad?.Trim() ?? string.Empty;
        var hata = await AdHatasiAsync(ad, haricId: null);
        if (hata != null)
        {
            TempData["Error"] = hata;
            return RedirectToAction("Index");
        }

        _context.Kategoriler.Add(new Kategori { Ad = ad, OlusturmaTarihi = DateTime.Now });
        await _context.SaveChangesAsync();
        await _logService.AddAsync(LogModul.Kategori, LogIslem.Olusturma, $"{ad} category was added.");
        TempData["Success"] = "Category added.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(int id, string? ad)
    {
        var kategori = await _context.Kategoriler.FindAsync(id);
        if (kategori == null) return NotFound();

        ad = ad?.Trim() ?? string.Empty;
        var hata = await AdHatasiAsync(ad, haricId: id);
        if (hata != null)
        {
            TempData["Error"] = hata;
            return RedirectToAction("Index");
        }

        var eskiAd = kategori.Ad;
        kategori.Ad = ad;
        await _context.SaveChangesAsync();
        await _logService.AddAsync(LogModul.Kategori, LogIslem.Guncelleme, $"{eskiAd} category was renamed to {ad}.");
        TempData["Success"] = "Category renamed. All linked products were updated automatically.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var kategori = await _context.Kategoriler.FindAsync(id);
        if (kategori == null) return NotFound();

        if (await _context.Urunler.AnyAsync(u => u.KategoriId == id))
        {
            TempData["Error"] = "This category has product history and cannot be deleted. You can rename it instead.";
            return RedirectToAction("Index");
        }

        _context.Kategoriler.Remove(kategori);
        await _context.SaveChangesAsync();
        await _logService.AddAsync(LogModul.Kategori, LogIslem.Silme, $"{kategori.Ad} category was deleted.");
        TempData["Success"] = "Category deleted.";
        return RedirectToAction("Index");
    }

    private async Task<string?> AdHatasiAsync(string ad, int? haricId)
    {
        if (string.IsNullOrWhiteSpace(ad)) return "Category name is required.";
        if (ad.Length > 100) return "Category name can be at most 100 characters.";

        var normalized = ad.ToLower();
        var varMi = await _context.Kategoriler.AnyAsync(k => k.Ad.ToLower() == normalized && k.Id != haricId);
        return varMi ? "This category is already registered." : null;
    }
}
