using Microsoft.EntityFrameworkCore;
using MiniERP.Entities;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Tests;

public class SiparisServiceTests
{
    private static SiparisViewModel Siparis(int musteriId, params (int UrunId, int Miktar)[] kalemler) => new()
    {
        MusteriId = musteriId,
        Kalemler = kalemler.Select(k => new SiparisKalemi { UrunId = k.UrunId, Miktar = k.Miktar }).ToList()
    };

    [Fact]
    public async Task Creating_an_order_reserves_stock_and_calculates_the_total()
    {
        var db = TestDb.CreateSeeded(out var ids);
        await using var ctx = db.NewContext();

        var sonuc = await db.NewSiparisService(ctx).OlusturAsync(Siparis(ids.MusteriId, (ids.UrunA, 3), (ids.UrunB, 2)));

        Assert.True(sonuc.Basarili);
        await using var kontrol = db.NewContext();
        var siparis = await kontrol.Siparisler.Include(s => s.SiparisDetaylar).SingleAsync();
        Assert.Equal(SiparisDurumu.Bekliyor, siparis.Durum);
        Assert.Equal(3 * 100m + 2 * 25.5m, siparis.ToplamTutar);
        Assert.Equal(7, (await kontrol.Urunler.FindAsync(ids.UrunA))!.StokAdedi);
        Assert.Equal(2, (await kontrol.Urunler.FindAsync(ids.UrunB))!.StokAdedi);
        Assert.Single(kontrol.IslemLoglari);
    }

    [Fact]
    public async Task Duplicate_lines_for_the_same_product_are_merged()
    {
        var db = TestDb.CreateSeeded(out var ids);
        await using var ctx = db.NewContext();

        var sonuc = await db.NewSiparisService(ctx).OlusturAsync(Siparis(ids.MusteriId, (ids.UrunA, 2), (ids.UrunA, 3)));

        Assert.True(sonuc.Basarili);
        await using var kontrol = db.NewContext();
        var detay = Assert.Single(kontrol.SiparisDetaylar);
        Assert.Equal(5, detay.Miktar);
    }

    [Fact]
    public async Task Ordering_more_than_available_stock_fails_without_changes()
    {
        var db = TestDb.CreateSeeded(out var ids);
        await using var ctx = db.NewContext();

        // Two lines that are individually fine but together exceed stock (4).
        var sonuc = await db.NewSiparisService(ctx).OlusturAsync(Siparis(ids.MusteriId, (ids.UrunB, 3), (ids.UrunB, 2)));

        Assert.False(sonuc.Basarili);
        Assert.Contains(sonuc.Hatalar, h => h.Contains("Insufficient stock"));
        await using var kontrol = db.NewContext();
        Assert.Empty(kontrol.Siparisler);
        Assert.Equal(4, (await kontrol.Urunler.FindAsync(ids.UrunB))!.StokAdedi);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Zero_or_negative_quantities_are_rejected(int miktar)
    {
        var db = TestDb.CreateSeeded(out var ids);
        await using var ctx = db.NewContext();

        var sonuc = await db.NewSiparisService(ctx).OlusturAsync(Siparis(ids.MusteriId, (ids.UrunA, miktar)));

        Assert.False(sonuc.Basarili);
    }

    [Fact]
    public async Task Inactive_products_and_unknown_customers_are_rejected()
    {
        var db = TestDb.CreateSeeded(out var ids);
        await using var ctx = db.NewContext();
        var service = db.NewSiparisService(ctx);

        Assert.False((await service.OlusturAsync(Siparis(ids.MusteriId, (ids.PasifUrun, 1)))).Basarili);
        Assert.False((await service.OlusturAsync(Siparis(9999, (ids.UrunA, 1)))).Basarili);
        Assert.False((await service.OlusturAsync(Siparis(ids.MusteriId))).Basarili);
    }

    [Fact]
    public async Task Canceling_an_order_returns_reserved_stock()
    {
        var db = TestDb.CreateSeeded(out var ids);
        int siparisId;
        await using (var ctx = db.NewContext())
        {
            siparisId = (await db.NewSiparisService(ctx).OlusturAsync(Siparis(ids.MusteriId, (ids.UrunA, 4)))).Id!.Value;
        }

        await using (var ctx = db.NewContext())
        {
            var sonuc = await db.NewSiparisService(ctx).DurumGuncelleAsync(siparisId, SiparisDurumu.Iptal);
            Assert.True(sonuc.Basarili);
        }

        await using var kontrol = db.NewContext();
        Assert.Equal(SiparisDurumu.Iptal, (await kontrol.Siparisler.FindAsync(siparisId))!.Durum);
        Assert.Equal(10, (await kontrol.Urunler.FindAsync(ids.UrunA))!.StokAdedi);
    }

    [Fact]
    public async Task An_order_cannot_be_completed_until_its_invoice_is_paid()
    {
        var db = TestDb.CreateSeeded(out var ids);
        int siparisId;
        await using (var ctx = db.NewContext())
        {
            var service = db.NewSiparisService(ctx);
            siparisId = (await service.OlusturAsync(Siparis(ids.MusteriId, (ids.UrunA, 1)))).Id!.Value;
            Assert.True((await service.DurumGuncelleAsync(siparisId, SiparisDurumu.Hazirlaniyor)).Basarili);

            var faturasiz = await service.DurumGuncelleAsync(siparisId, SiparisDurumu.Tamamlandi);
            Assert.False(faturasiz.Basarili);
            Assert.Contains("invoice", faturasiz.IlkHata);
        }

        await using (var ctx = db.NewContext())
        {
            ctx.Faturalar.Add(new Fatura { SiparisId = siparisId, FaturaNo = "FTR-TEST-1", ToplamTutar = 100m, OdenmeDurumu = false });
            await ctx.SaveChangesAsync();
            Assert.False((await db.NewSiparisService(ctx).DurumGuncelleAsync(siparisId, SiparisDurumu.Tamamlandi)).Basarili);

            var fatura = await ctx.Faturalar.SingleAsync();
            fatura.OdenmeDurumu = true;
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = db.NewContext())
        {
            Assert.True((await db.NewSiparisService(ctx).DurumGuncelleAsync(siparisId, SiparisDurumu.Tamamlandi)).Basarili);
        }
    }

    [Fact]
    public async Task Invalid_transitions_are_rejected()
    {
        var db = TestDb.CreateSeeded(out var ids);
        await using var ctx = db.NewContext();
        var service = db.NewSiparisService(ctx);
        var siparisId = (await service.OlusturAsync(Siparis(ids.MusteriId, (ids.UrunA, 1)))).Id!.Value;

        var sonuc = await service.DurumGuncelleAsync(siparisId, SiparisDurumu.Tamamlandi);

        Assert.False(sonuc.Basarili);
        Assert.Contains("cannot be moved", sonuc.IlkHata);
    }

    [Fact]
    public async Task Deleting_a_pending_order_returns_stock_but_invoiced_orders_are_protected()
    {
        var db = TestDb.CreateSeeded(out var ids);
        int silinecek, faturali;
        await using (var ctx = db.NewContext())
        {
            var service = db.NewSiparisService(ctx);
            silinecek = (await service.OlusturAsync(Siparis(ids.MusteriId, (ids.UrunA, 2)))).Id!.Value;
            faturali = (await service.OlusturAsync(Siparis(ids.MusteriId, (ids.UrunA, 3)))).Id!.Value;
            ctx.Faturalar.Add(new Fatura { SiparisId = faturali, FaturaNo = "FTR-TEST-2", ToplamTutar = 300m });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = db.NewContext())
        {
            var service = db.NewSiparisService(ctx);
            Assert.True((await service.SilAsync(silinecek)).Basarili);
            Assert.False((await service.SilAsync(faturali)).Basarili);
        }

        await using var kontrol = db.NewContext();
        Assert.Equal(new[] { faturali }, kontrol.Siparisler.Select(s => s.Id).ToArray());
        Assert.Equal(7, (await kontrol.Urunler.FindAsync(ids.UrunA))!.StokAdedi);
    }

    [Fact]
    public async Task Concurrent_stock_change_prevents_overselling()
    {
        var db = TestDb.CreateSeeded(out var ids);
        await using var ctx = db.NewContext();

        // Request 1 has already read the product (stock = 10)...
        await ctx.Urunler.FindAsync(ids.UrunA);

        // ...while request 2 sells 8 units and saves first.
        await using (var other = db.NewContext())
        {
            (await other.Urunler.FindAsync(ids.UrunA))!.StokAdedi = 2;
            await other.SaveChangesAsync();
        }

        var sonuc = await db.NewSiparisService(ctx).OlusturAsync(Siparis(ids.MusteriId, (ids.UrunA, 5)));

        Assert.False(sonuc.Basarili);
        Assert.Equal(SiparisService.StokDegistiMesaji, sonuc.IlkHata);
        await using var kontrol = db.NewContext();
        Assert.Equal(2, (await kontrol.Urunler.FindAsync(ids.UrunA))!.StokAdedi);
        // Note: the in-memory provider is not transactional, so the order row itself is only rolled
        // back on a relational database (PostgreSQL wraps SaveChanges + the explicit transaction).
    }
}
