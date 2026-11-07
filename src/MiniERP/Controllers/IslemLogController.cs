using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Models;

namespace MiniERP.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Yonetici}")]
public class IslemLogController : Controller
{
    private readonly AppDbContext _context;

    public IslemLogController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? modul, DateTime? baslangicTarihi, DateTime? bitisTarihi)
    {
        var query = _context.IslemLoglari.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(modul))
        {
            query = query.Where(l => l.Modul == modul);
        }

        if (baslangicTarihi.HasValue)
        {
            var baslangic = baslangicTarihi.Value.Date;
            query = query.Where(l => l.Tarih >= baslangic);
        }

        if (bitisTarihi.HasValue)
        {
            var bitis = bitisTarihi.Value.Date.AddDays(1);
            query = query.Where(l => l.Tarih < bitis);
        }

        ViewBag.Moduller = await _context.IslemLoglari
            .AsNoTracking()
            .Select(l => l.Modul)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
        ViewBag.SeciliModul = modul;
        ViewBag.BaslangicTarihi = baslangicTarihi;
        ViewBag.BitisTarihi = bitisTarihi;

        var loglar = await query
            .OrderByDescending(l => l.Tarih)
            .ThenByDescending(l => l.Id)
            .Take(500)
            .ToListAsync();

        return View(loglar);
    }
}
