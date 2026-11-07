using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniERP.Entities;
using MiniERP.Models;

namespace MiniERP.Data.Seed;

public static class DbSeeder
{
    private static readonly (string Email, string AdSoyad, string Birim)[] DemoKullanicilar =
    [
        ("sales@minierp.com", "Selin Satış", AppRoles.BirimSatis),
        ("stock@minierp.com", "Serkan Stok", AppRoles.BirimStok),
        ("accounting@minierp.com", "Merve Muhasebe", AppRoles.BirimMuhasebe),
        ("manager@minierp.com", "Mehmet Yönetici", AppRoles.BirimYonetici)
    ];

    public static async Task SeedAsync(IServiceProvider services, bool migrate)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));
        var context = provider.GetRequiredService<AppDbContext>();
        var options = provider.GetRequiredService<IOptions<SeedOptions>>().Value;

        if (migrate)
        {
            await context.Database.MigrateAsync();
        }

        await SeedRolesAndUsersAsync(provider, options, logger);

        if (options.DemoData)
        {
            await SeedDemoDataAsync(context);
        }
    }

    private static async Task SeedRolesAndUsersAsync(IServiceProvider provider, SeedOptions options, ILogger logger)
    {
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            if (await userManager.FindByEmailAsync(options.AdminEmail) == null)
            {
                logger.LogWarning("Seed:AdminPassword is not configured; the admin account was not created.");
            }
        }
        else
        {
            await EnsureUserAsync(userManager, logger, options.AdminEmail, options.AdminPassword,
                "System Admin", AppRoles.BirimYonetici, AppRoles.Admin);
        }

        if (options.DemoUsers && !string.IsNullOrWhiteSpace(options.DemoUserPassword))
        {
            foreach (var (email, adSoyad, birim) in DemoKullanicilar)
            {
                await EnsureUserAsync(userManager, logger, email, options.DemoUserPassword,
                    adSoyad, birim, AppRoles.GetRoleForBirim(birim)!);
            }
        }
    }

    /// <summary>Creates the user if missing; never overwrites an existing user's password or profile.</summary>
    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, ILogger logger,
        string email, string password, string adSoyad, string birim, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                AdSoyad = adSoyad,
                Birim = birim
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Seed user {Email} could not be created: {Errors}", email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    private static async Task SeedDemoDataAsync(AppDbContext context)
    {
        if (await context.Musteriler.AnyAsync() || await context.Urunler.AnyAsync())
        {
            return;
        }

        var hammadde = new Kategori { Ad = "Raw Material" };
        var araUrun = new Kategori { Ad = "Intermediate Product" };
        var ambalaj = new Kategori { Ad = "Packaging" };
        var temizlik = new Kategori { Ad = "Cleaning Chemicals" };
        context.Kategoriler.AddRange(hammadde, araUrun, ambalaj, temizlik);

        var musteriler = new[]
        {
            new Musteri { Ad = "Nova Hospital", Email = "purchasing@novahospital.com", Telefon = "0212 444 10 10", Adres = "Istanbul" },
            new Musteri { Ad = "Marmara Chemical Inc.", Email = "operations@marmarachemical.com", Telefon = "0262 555 22 33", Adres = "Kocaeli" },
            new Musteri { Ad = "Anka Cleaning Products", Email = "supply@ankacleaning.com", Telefon = "0312 555 44 55", Adres = "Ankara" }
        };
        context.Musteriler.AddRange(musteriler);

        var urunler = new[]
        {
            new Urun { Ad = "Sodium Hydroxide", Kategori = hammadde, Fiyat = 1850, StokAdedi = 18 },
            new Urun { Ad = "Ethanol", Kategori = hammadde, Fiyat = 2400, StokAdedi = 7 },
            new Urun { Ad = "pH Regulator Blend", Kategori = araUrun, Fiyat = 3150, StokAdedi = 5 },
            new Urun { Ad = "HDPE Canister 20 L", Kategori = ambalaj, Fiyat = 95, StokAdedi = 120 },
            new Urun { Ad = "Industrial Surface Cleaner", Kategori = temizlik, Fiyat = 680, StokAdedi = 26 },
            new Urun { Ad = "Citric Acid", Kategori = hammadde, Fiyat = 940, StokAdedi = 3 }
        };
        context.Urunler.AddRange(urunler);

        // Order history covering every status so the dashboard and reports have data.
        var plan = new (Musteri Musteri, int GunOnce, SiparisDurumu Durum, (Urun Urun, int Miktar)[] Kalemler, bool? Odendi)[]
        {
            (musteriler[0], 95, SiparisDurumu.Tamamlandi, [(urunler[0], 3), (urunler[3], 10)], true),
            (musteriler[1], 70, SiparisDurumu.Tamamlandi, [(urunler[1], 4), (urunler[2], 2)], true),
            (musteriler[2], 40, SiparisDurumu.Tamamlandi, [(urunler[4], 12), (urunler[3], 20)], true),
            (musteriler[0], 12, SiparisDurumu.Hazirlaniyor, [(urunler[4], 6)], false),
            (musteriler[1], 5, SiparisDurumu.Iptal, [(urunler[0], 2)], null),
            (musteriler[2], 1, SiparisDurumu.Bekliyor, [(urunler[3], 15), (urunler[5], 1)], null)
        };

        var siraNo = 1;
        foreach (var (musteri, gunOnce, durum, kalemler, odendi) in plan)
        {
            var tarih = DateTime.Now.AddDays(-gunOnce);
            var siparis = new Siparis { Musteri = musteri, SiparisTarihi = tarih, Durum = durum };
            foreach (var (urun, miktar) in kalemler)
            {
                siparis.SiparisDetaylar.Add(new SiparisDetay { Urun = urun, Miktar = miktar, BirimFiyat = urun.Fiyat });
            }
            siparis.ToplamTutar = siparis.SiparisDetaylar.Sum(d => d.Miktar * d.BirimFiyat);
            context.Siparisler.Add(siparis);

            if (odendi.HasValue)
            {
                context.Faturalar.Add(new Fatura
                {
                    Siparis = siparis,
                    FaturaNo = $"FTR-DEMO-{siraNo:D4}",
                    FaturaTarihi = tarih.AddDays(1),
                    ToplamTutar = siparis.ToplamTutar,
                    OdenmeDurumu = odendi.Value
                });
            }

            siraNo++;
        }

        await context.SaveChangesAsync();
    }
}
