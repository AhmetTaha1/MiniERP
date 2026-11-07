using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MiniERP.Data;
using MiniERP.Entities;
using MiniERP.Services;

namespace MiniERP.Tests;

/// <summary>Creates isolated in-memory databases with a small, known data set.</summary>
internal sealed class TestDb
{
    private readonly string _name = Guid.NewGuid().ToString();

    public AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_name)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    public SiparisService NewSiparisService(AppDbContext context) =>
        new(context, new IslemLogService(context, new HttpContextAccessor()));

    public static TestDb CreateSeeded(out SeedIds ids)
    {
        var db = new TestDb();
        using var ctx = db.NewContext();

        var kategori = new Kategori { Ad = "Raw Material" };
        var musteri = new Musteri { Ad = "Nova Hospital", Email = "nova@example.com" };
        var urunA = new Urun { Ad = "Ethanol", Kategori = kategori, Fiyat = 100m, StokAdedi = 10 };
        var urunB = new Urun { Ad = "Citric Acid", Kategori = kategori, Fiyat = 25.5m, StokAdedi = 4 };
        var pasif = new Urun { Ad = "Old Product", Kategori = kategori, Fiyat = 1m, StokAdedi = 50, Aktif = false };
        ctx.AddRange(kategori, musteri, urunA, urunB, pasif);
        ctx.SaveChanges();

        ids = new SeedIds(musteri.Id, urunA.Id, urunB.Id, pasif.Id);
        return db;
    }
}

internal sealed record SeedIds(int MusteriId, int UrunA, int UrunB, int PasifUrun);
