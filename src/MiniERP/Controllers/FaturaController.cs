using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Entities;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Muhasebe}")]
public class FaturaController : Controller
{
    private readonly AppDbContext _context;
    private readonly IIslemLogService _logService;

    public FaturaController(AppDbContext context, IIslemLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    public async Task<IActionResult> Index(bool? odenmeDurumu, DateTime? baslangicTarihi, DateTime? bitisTarihi)
    {
        var query = _context.Faturalar
            .AsNoTracking()
            .Include(f => f.Siparis)
                .ThenInclude(s => s.Musteri)
            .AsQueryable();

        if (odenmeDurumu.HasValue)
        {
            query = query.Where(f => f.OdenmeDurumu == odenmeDurumu.Value);
        }

        if (baslangicTarihi.HasValue)
        {
            var baslangic = baslangicTarihi.Value.Date;
            query = query.Where(f => f.FaturaTarihi >= baslangic);
        }

        if (bitisTarihi.HasValue)
        {
            var bitis = bitisTarihi.Value.Date.AddDays(1);
            query = query.Where(f => f.FaturaTarihi < bitis);
        }

        ViewBag.OdenmeDurumu = odenmeDurumu;
        ViewBag.BaslangicTarihi = baslangicTarihi;
        ViewBag.BitisTarihi = bitisTarihi;

        var faturalar = await query
            .OrderByDescending(f => f.FaturaTarihi)
            .ThenByDescending(f => f.Id)
            .ToListAsync();
        return View(faturalar);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int siparisId)
    {
        var siparis = await _context.Siparisler
            .Include(s => s.Fatura)
            .FirstOrDefaultAsync(s => s.Id == siparisId);

        if (siparis == null) return NotFound();

        if (siparis.Durum == SiparisDurumu.Iptal)
        {
            TempData["Error"] = "An invoice cannot be created for a canceled order.";
            return RedirectToAction("Detay", "Siparis", new { id = siparisId });
        }

        if (siparis.Fatura != null)
        {
            TempData["Error"] = "An invoice has already been created for this order.";
            return RedirectToAction("Detay", "Siparis", new { id = siparisId });
        }

        var fatura = new Fatura
        {
            SiparisId = siparisId,
            FaturaNo = await YeniFaturaNoAsync(),
            FaturaTarihi = DateTime.Now,
            ToplamTutar = siparis.ToplamTutar,
            OdenmeDurumu = false
        };

        _context.Faturalar.Add(fatura);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique index on SiparisId protects against double submission.
            TempData["Error"] = "An invoice has already been created for this order.";
            return RedirectToAction("Detay", "Siparis", new { id = siparisId });
        }

        await _logService.AddAsync(LogModul.Fatura, LogIslem.Olusturma, $"{fatura.FaturaNo} invoice was created.");
        TempData["Success"] = $"Invoice {fatura.FaturaNo} created.";
        return RedirectToAction("Detay", new { id = fatura.Id });
    }

    public async Task<IActionResult> Detay(int id)
    {
        var fatura = await FaturaDetaylariylaGetirAsync(id);
        if (fatura == null) return NotFound();
        return View(fatura);
    }

    public async Task<IActionResult> Pdf(int id)
    {
        var fatura = await FaturaDetaylariylaGetirAsync(id);
        if (fatura == null) return NotFound();

        var musteri = fatura.Siparis.Musteri;
        var lines = new List<string>
        {
            $"Invoice No: {fatura.FaturaNo}",
            $"Date: {fatura.FaturaTarihi:dd.MM.yyyy HH:mm}",
            $"Order: #{fatura.SiparisId}",
            $"Customer: {musteri.AdSoyad}",
            $"Email: {musteri.Email}",
            $"Phone: {musteri.Telefon}",
            $"Payment Status: {(fatura.OdenmeDurumu ? "Paid" : "Unpaid")}",
            string.Empty,
            "Invoice Lines:"
        };

        lines.AddRange(fatura.Siparis.SiparisDetaylar.Select(d =>
            $"- {d.Urun.Ad} | {d.Miktar} units x {DisplayText.Money(d.BirimFiyat)} = {DisplayText.Money(d.Miktar * d.BirimFiyat)}"));
        lines.Add(string.Empty);
        lines.Add($"Grand Total: {DisplayText.Money(fatura.ToplamTutar)}");

        var pdf = SimplePdfService.Create("MiniERP Invoice", lines);
        return File(pdf, "application/pdf", $"{fatura.FaturaNo}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OdemeDurumDegistir(int id, bool detay = false)
    {
        var fatura = await _context.Faturalar
            .Include(f => f.Siparis)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (fatura == null) return NotFound();

        if (fatura.OdenmeDurumu && fatura.Siparis.Durum == SiparisDurumu.Tamamlandi)
        {
            TempData["Error"] = "The paid invoice of a completed order cannot be marked as unpaid again.";
        }
        else
        {
            fatura.OdenmeDurumu = !fatura.OdenmeDurumu;
            await _context.SaveChangesAsync();
            await _logService.AddAsync(LogModul.Fatura, LogIslem.OdemeDurumu,
                $"{fatura.FaturaNo} payment status was set to {(fatura.OdenmeDurumu ? "paid" : "unpaid")}.");
        }

        return detay ? RedirectToAction("Detay", new { id }) : RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var fatura = await _context.Faturalar.FindAsync(id);
        if (fatura == null) return NotFound();

        if (fatura.OdenmeDurumu)
        {
            TempData["Error"] = "Paid invoices cannot be deleted. Reverse the payment status first.";
            return RedirectToAction("Index");
        }

        _context.Faturalar.Remove(fatura);
        await _context.SaveChangesAsync();
        await _logService.AddAsync(LogModul.Fatura, LogIslem.Silme, $"{fatura.FaturaNo} invoice was deleted.");
        return RedirectToAction("Index");
    }

    private Task<Fatura?> FaturaDetaylariylaGetirAsync(int id) =>
        _context.Faturalar
            .AsNoTracking()
            .Include(f => f.Siparis)
                .ThenInclude(s => s.Musteri)
            .Include(f => f.Siparis)
                .ThenInclude(s => s.SiparisDetaylar)
                    .ThenInclude(d => d.Urun)
                        .ThenInclude(u => u.Kategori)
            .FirstOrDefaultAsync(f => f.Id == id);

    /// <summary>Sequential, human-readable invoice numbers: FTR-2026-000123.</summary>
    private async Task<string> YeniFaturaNoAsync()
    {
        var onEk = $"FTR-{DateTime.Now:yyyy}-";
        var sonNo = await _context.Faturalar
            .Where(f => f.FaturaNo.StartsWith(onEk))
            .Select(f => f.FaturaNo)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync();

        var sira = sonNo != null && int.TryParse(sonNo[onEk.Length..], out var n) ? n + 1 : 1;
        return $"{onEk}{sira:D6}";
    }
}
