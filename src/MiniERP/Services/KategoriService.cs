using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Entities;

namespace MiniERP.Services;

public interface IKategoriService
{
    /// <summary>
    /// Resolves a category by name (case-insensitive). If it does not exist it is added to the
    /// change tracker and will be saved together with the caller's next SaveChanges.
    /// </summary>
    Task<Kategori?> BulVeyaEkleAsync(string? mevcutKategori, string? yeniKategori);
}

public class KategoriService : IKategoriService
{
    private readonly AppDbContext _context;

    public KategoriService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Kategori?> BulVeyaEkleAsync(string? mevcutKategori, string? yeniKategori)
    {
        var ad = !string.IsNullOrWhiteSpace(yeniKategori) ? yeniKategori.Trim() : mevcutKategori?.Trim();
        if (string.IsNullOrWhiteSpace(ad))
        {
            return null;
        }

        var normalized = ad.ToLower();
        var kategori = await _context.Kategoriler.FirstOrDefaultAsync(k => k.Ad.ToLower() == normalized);
        if (kategori != null)
        {
            return kategori;
        }

        kategori = new Kategori { Ad = ad, OlusturmaTarihi = DateTime.Now };
        _context.Kategoriler.Add(kategori);
        return kategori;
    }
}
