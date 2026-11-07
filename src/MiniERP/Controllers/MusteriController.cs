using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Entities;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Satis}")]
public class MusteriController : Controller
{
    private readonly AppDbContext _context;
    private readonly IIslemLogService _logService;

    public MusteriController(AppDbContext context, IIslemLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.SiparisSayilari = await _context.Siparisler
            .AsNoTracking()
            .GroupBy(s => s.MusteriId)
            .Select(g => new { MusteriId = g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.MusteriId, x => x.Adet);

        var musteriler = await _context.Musteriler
            .AsNoTracking()
            .OrderByDescending(m => m.OlusturmaTarihi)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return View(musteriler);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Ad,Soyad,Email,Telefon,Adres")] Musteri musteri)
    {
        AlanlariTemizle(musteri);

        if (await EmailKullanimdaMiAsync(musteri.Email, haricId: null))
        {
            ModelState.AddModelError(nameof(musteri.Email), "A customer is already registered with this email address.");
        }

        if (!ModelState.IsValid)
        {
            return View(musteri);
        }

        musteri.OlusturmaTarihi = DateTime.Now;
        _context.Musteriler.Add(musteri);
        await _context.SaveChangesAsync();
        await _logService.AddAsync(LogModul.Musteri, LogIslem.Olusturma, $"{musteri.AdSoyad} customer was added.");
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var musteri = await _context.Musteriler.FindAsync(id);
        if (musteri == null) return NotFound();
        return View(musteri);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Ad,Soyad,Email,Telefon,Adres")] Musteri form)
    {
        if (id != form.Id) return BadRequest();

        AlanlariTemizle(form);

        if (await EmailKullanimdaMiAsync(form.Email, haricId: id))
        {
            ModelState.AddModelError(nameof(form.Email), "Another customer is already registered with this email address.");
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var musteri = await _context.Musteriler.FindAsync(id);
        if (musteri == null) return NotFound();

        // Copy only editable fields so OlusturmaTarihi cannot be overwritten by the form.
        musteri.Ad = form.Ad;
        musteri.Soyad = form.Soyad;
        musteri.Email = form.Email;
        musteri.Telefon = form.Telefon;
        musteri.Adres = form.Adres;

        await _context.SaveChangesAsync();
        await _logService.AddAsync(LogModul.Musteri, LogIslem.Guncelleme, $"{musteri.AdSoyad} customer was updated.");
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var musteri = await _context.Musteriler.FindAsync(id);
        if (musteri == null) return NotFound();

        if (await _context.Siparisler.AnyAsync(s => s.MusteriId == id))
        {
            TempData["Error"] = $"{musteri.AdSoyad} has order history and cannot be deleted. Orders and invoices are preserved for ERP record integrity.";
            return RedirectToAction("Index");
        }

        _context.Musteriler.Remove(musteri);
        await _context.SaveChangesAsync();
        await _logService.AddAsync(LogModul.Musteri, LogIslem.Silme, $"{musteri.AdSoyad} customer was deleted.");
        TempData["Success"] = "Customer deleted.";
        return RedirectToAction("Index");
    }

    private Task<bool> EmailKullanimdaMiAsync(string email, int? haricId)
    {
        var normalized = email.ToLower();
        return _context.Musteriler.AnyAsync(m => m.Email.ToLower() == normalized && m.Id != haricId);
    }

    private static void AlanlariTemizle(Musteri musteri)
    {
        musteri.Ad = musteri.Ad?.Trim() ?? string.Empty;
        musteri.Soyad = string.IsNullOrWhiteSpace(musteri.Soyad) ? null : musteri.Soyad.Trim();
        musteri.Email = musteri.Email?.Trim() ?? string.Empty;
        musteri.Telefon = musteri.Telefon?.Trim() ?? string.Empty;
        musteri.Adres = musteri.Adres?.Trim() ?? string.Empty;
    }
}
