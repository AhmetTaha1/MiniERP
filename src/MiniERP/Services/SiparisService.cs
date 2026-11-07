using Microsoft.EntityFrameworkCore;
using MiniERP.Data;
using MiniERP.Entities;
using MiniERP.Models;

namespace MiniERP.Services;

public interface ISiparisService
{
    Task<IslemSonucu> OlusturAsync(SiparisViewModel model);
    Task<IslemSonucu> DurumGuncelleAsync(int siparisId, SiparisDurumu yeniDurum);
    Task<IslemSonucu> SilAsync(int siparisId);
    Task<(bool Tamamlanabilir, string Mesaj)> TamamlanabilirMiAsync(int siparisId);
}

public class SiparisService : ISiparisService
{
    public const string StokDegistiMesaji = "Stock levels changed while your request was being processed. Please review the order and try again.";

    private readonly AppDbContext _context;
    private readonly IIslemLogService _logService;

    public SiparisService(AppDbContext context, IIslemLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    public async Task<IslemSonucu> OlusturAsync(SiparisViewModel model)
    {
        var hatalar = new List<string>();

        if (model.MusteriId <= 0 || !await _context.Musteriler.AnyAsync(m => m.Id == model.MusteriId))
        {
            hatalar.Add("Select a customer.");
        }

        var kalemler = model.Kalemler.Where(k => k.UrunId > 0).ToList();
        if (kalemler.Count == 0)
        {
            hatalar.Add("Add at least one product.");
        }

        if (kalemler.Any(k => k.Miktar <= 0))
        {
            hatalar.Add("Order quantity cannot be zero or negative.");
        }

        var urunIds = kalemler.Select(k => k.UrunId).Distinct().ToList();
        var urunler = await _context.Urunler
            .Where(u => urunIds.Contains(u.Id) && u.Aktif)
            .ToDictionaryAsync(u => u.Id);

        foreach (var grup in kalemler.GroupBy(k => k.UrunId))
        {
            if (!urunler.TryGetValue(grup.Key, out var urun))
            {
                hatalar.Add("One of the selected products could not be found.");
                continue;
            }

            var istenen = grup.Sum(k => k.Miktar);
            if (istenen > urun.StokAdedi)
            {
                hatalar.Add($"Insufficient stock for {urun.Ad}. {urun.StokAdedi} units are available.");
            }
        }

        if (hatalar.Count > 0)
        {
            return IslemSonucu.Hata(hatalar);
        }

        // Merge duplicate lines of the same product into a single order line.
        var siparis = new Siparis
        {
            MusteriId = model.MusteriId,
            SiparisTarihi = DateTime.Now,
            Durum = SiparisDurumu.Bekliyor
        };

        foreach (var grup in kalemler.GroupBy(k => k.UrunId))
        {
            var urun = urunler[grup.Key];
            var miktar = grup.Sum(k => k.Miktar);

            siparis.SiparisDetaylar.Add(new SiparisDetay
            {
                UrunId = urun.Id,
                Miktar = miktar,
                BirimFiyat = urun.Fiyat
            });
            urun.StokAdedi -= miktar;
        }

        siparis.ToplamTutar = siparis.SiparisDetaylar.Sum(d => d.Miktar * d.BirimFiyat);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Siparisler.Add(siparis);
            await _context.SaveChangesAsync();
            await _logService.AddAsync(LogModul.Siparis, LogIslem.Olusturma, $"Order #{siparis.Id} was created.");
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();
            return IslemSonucu.Hata(StokDegistiMesaji);
        }

        return IslemSonucu.Ok(siparis.Id);
    }

    public async Task<IslemSonucu> DurumGuncelleAsync(int siparisId, SiparisDurumu yeniDurum)
    {
        var siparis = await DetaylariylaGetirAsync(siparisId);
        if (siparis == null)
        {
            return IslemSonucu.Hata("Order not found.");
        }

        if (siparis.Durum == yeniDurum)
        {
            return IslemSonucu.Ok(siparis.Id);
        }

        if (!SiparisDurumKurallari.GecisGecerliMi(siparis.Durum, yeniDurum))
        {
            return IslemSonucu.Hata(
                $"An order in {DisplayText.OrderStatus(siparis.Durum)} status cannot be moved to {DisplayText.OrderStatus(yeniDurum)} status.");
        }

        if (yeniDurum == SiparisDurumu.Tamamlandi)
        {
            var (tamamlanabilir, mesaj) = await TamamlanabilirMiAsync(siparis.Id);
            if (!tamamlanabilir)
            {
                return IslemSonucu.Hata(mesaj);
            }
        }

        if (SiparisDurumKurallari.StokIadesiGerekirMi(siparis.Durum, yeniDurum))
        {
            foreach (var detay in siparis.SiparisDetaylar)
            {
                detay.Urun.StokAdedi += detay.Miktar;
            }
        }

        siparis.Durum = yeniDurum;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();
            await _logService.AddAsync(LogModul.Siparis, LogIslem.DurumGuncelleme,
                $"Order #{siparis.Id} was moved to {DisplayText.OrderStatus(yeniDurum)} status.");
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();
            return IslemSonucu.Hata(StokDegistiMesaji);
        }

        return IslemSonucu.Ok(siparis.Id);
    }

    public async Task<IslemSonucu> SilAsync(int siparisId)
    {
        var siparis = await DetaylariylaGetirAsync(siparisId);
        if (siparis == null)
        {
            return IslemSonucu.Hata("Order not found.");
        }

        if (siparis.Durum == SiparisDurumu.Tamamlandi || siparis.Fatura != null)
        {
            return IslemSonucu.Hata("Completed or invoiced orders cannot be deleted. This sale is preserved for ERP record integrity.");
        }

        if (siparis.Durum != SiparisDurumu.Iptal)
        {
            foreach (var detay in siparis.SiparisDetaylar)
            {
                detay.Urun.StokAdedi += detay.Miktar;
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Siparisler.Remove(siparis);
            await _context.SaveChangesAsync();
            await _logService.AddAsync(LogModul.Siparis, LogIslem.Silme, $"Order #{siparis.Id} was deleted.");
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();
            return IslemSonucu.Hata(StokDegistiMesaji);
        }

        return IslemSonucu.Ok(siparis.Id);
    }

    public async Task<(bool Tamamlanabilir, string Mesaj)> TamamlanabilirMiAsync(int siparisId)
    {
        var fatura = await _context.Faturalar
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.SiparisId == siparisId);

        if (fatura == null)
        {
            return (false, "Create an invoice before completing the order.");
        }

        if (!fatura.OdenmeDurumu)
        {
            return (false, "Mark the invoice as paid before completing the order.");
        }

        return (true, string.Empty);
    }

    private Task<Siparis?> DetaylariylaGetirAsync(int id) =>
        _context.Siparisler
            .Include(s => s.Fatura)
            .Include(s => s.SiparisDetaylar)
                .ThenInclude(d => d.Urun)
            .FirstOrDefaultAsync(s => s.Id == id);
}
