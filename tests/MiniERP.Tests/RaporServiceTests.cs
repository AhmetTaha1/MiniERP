using ClosedXML.Excel;
using MiniERP.Entities;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Tests;

public class RaporServiceTests
{
    private static async Task<TestDb> SeedSalesAsync()
    {
        var db = TestDb.CreateSeeded(out var ids);
        await using var ctx = db.NewContext();
        var urun = await ctx.Urunler.FindAsync(ids.UrunA);

        Siparis Yeni(SiparisDurumu durum, int miktar, int gunOnce) => new()
        {
            MusteriId = ids.MusteriId,
            Durum = durum,
            SiparisTarihi = DateTime.Today.AddDays(-gunOnce),
            ToplamTutar = miktar * 100m,
            SiparisDetaylar = { new SiparisDetay { UrunId = urun!.Id, Miktar = miktar, BirimFiyat = 100m } }
        };

        var tamamlanan = Yeni(SiparisDurumu.Tamamlandi, 2, 10);
        var bekleyen = Yeni(SiparisDurumu.Bekliyor, 1, 1);
        var iptal = Yeni(SiparisDurumu.Iptal, 5, 1);
        ctx.Siparisler.AddRange(tamamlanan, bekleyen, iptal);
        await ctx.SaveChangesAsync();

        ctx.Faturalar.AddRange(
            new Fatura { SiparisId = tamamlanan.Id, FaturaNo = "F1", ToplamTutar = 200m, OdenmeDurumu = true, FaturaTarihi = DateTime.Today.AddDays(-9) },
            new Fatura { SiparisId = bekleyen.Id, FaturaNo = "F2", ToplamTutar = 100m, OdenmeDurumu = false, FaturaTarihi = DateTime.Today });
        await ctx.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Report_excludes_canceled_orders_and_counts_only_paid_invoices_as_revenue()
    {
        var db = await SeedSalesAsync();
        await using var ctx = db.NewContext();

        var rapor = await new RaporService(ctx).OlusturAsync(new RaporFiltresi());

        Assert.Equal(200m, rapor.ToplamGelir);
        Assert.Equal(1, rapor.OdenmemisFaturaSayisi);
        Assert.Equal(100m, rapor.OdenmemisFaturaTutari);
        Assert.Equal(1, rapor.BekleyenSiparisSayisi);

        var urun = Assert.Single(rapor.KategoriUrunRaporu);
        Assert.Equal(3, urun.SatilanAdet); // 2 completed + 1 pending, canceled (5) excluded
        Assert.Equal(300m, urun.Tutar);

        var musteri = Assert.Single(rapor.MusteriSatisRaporu);
        Assert.Equal(2, musteri.SiparisSayisi);
    }

    [Fact]
    public async Task Date_and_category_filters_are_applied()
    {
        var db = await SeedSalesAsync();
        await using var ctx = db.NewContext();
        var service = new RaporService(ctx);

        var sonHafta = await service.OlusturAsync(new RaporFiltresi { BaslangicTarihi = DateTime.Today.AddDays(-3) });
        Assert.Equal(0m, sonHafta.ToplamGelir);
        Assert.Equal(1, Assert.Single(sonHafta.KategoriUrunRaporu).SatilanAdet);

        var baskaKategori = await service.OlusturAsync(new RaporFiltresi { Kategori = "Packaging" });
        Assert.Empty(baskaKategori.KategoriUrunRaporu);
    }

    [Fact]
    public async Task Dashboard_aggregates_match_the_data()
    {
        var db = await SeedSalesAsync();
        await using var ctx = db.NewContext();

        var dashboard = await new DashboardService(ctx).OlusturAsync();

        Assert.Equal(200m, dashboard.ToplamGelir);
        Assert.Equal(1, dashboard.BekleyenSiparisSayisi);
        Assert.Equal(1, dashboard.KritikStokSayisi); // Citric Acid: 4 units
        Assert.Equal(3, dashboard.SonSiparisler.Count);
        Assert.Contains(dashboard.SiparisDurumDagilimi, x => x.Etiket == "Canceled" && x.Deger == 1);
    }

    [Fact]
    public async Task Excel_export_produces_a_valid_workbook()
    {
        var db = await SeedSalesAsync();
        await using var ctx = db.NewContext();
        var rapor = await new RaporService(ctx).OlusturAsync(new RaporFiltresi());

        var bytes = RaporExportService.Excel(rapor);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(["Summary", "Sales by Category", "Sales by Customer", "Critical Stock"],
            workbook.Worksheets.Select(w => w.Name).ToArray());
        Assert.Equal(200m, workbook.Worksheet("Summary").Cell(5, 2).GetValue<decimal>());
        Assert.Equal("Ethanol", workbook.Worksheet("Sales by Category").Cell(2, 2).GetString());
    }
}
