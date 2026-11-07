using MiniERP.Data;
using MiniERP.Entities;

namespace MiniERP.Services;

public interface IIslemLogService
{
    /// <summary>Writes an audit entry for the current user and saves it immediately.</summary>
    Task AddAsync(string modul, string islem, string aciklama);
}

public class IslemLogService : IIslemLogService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IslemLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task AddAsync(string modul, string islem, string aciklama)
    {
        var kullanici = _httpContextAccessor.HttpContext?.User.Identity?.Name;

        _context.IslemLoglari.Add(new IslemLog
        {
            Tarih = DateTime.Now,
            Kullanici = string.IsNullOrWhiteSpace(kullanici) ? "System" : kullanici,
            Modul = modul,
            Islem = islem,
            Aciklama = aciklama
        });

        await _context.SaveChangesAsync();
    }
}
