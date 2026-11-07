using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Yonetici}")]
public class RaporController : Controller
{
    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly IRaporService _raporService;

    public RaporController(IRaporService raporService)
    {
        _raporService = raporService;
    }

    public async Task<IActionResult> Index(DateTime? baslangicTarihi, DateTime? bitisTarihi, string? category)
    {
        return View(await _raporService.OlusturAsync(Filtre(baslangicTarihi, bitisTarihi, category)));
    }

    public async Task<IActionResult> Excel(DateTime? baslangicTarihi, DateTime? bitisTarihi, string? category)
    {
        var model = await _raporService.OlusturAsync(Filtre(baslangicTarihi, bitisTarihi, category));
        return File(RaporExportService.Excel(model), XlsxMime, $"MiniERP_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    public async Task<IActionResult> Pdf(DateTime? baslangicTarihi, DateTime? bitisTarihi, string? category)
    {
        var model = await _raporService.OlusturAsync(Filtre(baslangicTarihi, bitisTarihi, category));
        return File(RaporExportService.Pdf(model), "application/pdf", $"MiniERP_Report_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    private static RaporFiltresi Filtre(DateTime? baslangic, DateTime? bitis, string? kategori) =>
        new() { BaslangicTarihi = baslangic, BitisTarihi = bitis, Kategori = kategori };
}
