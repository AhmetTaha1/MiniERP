using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniERP.Entities;

namespace MiniERP.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    static AppDbContext()
    {
        // The app works with server-local DateTime values. In legacy mode Npgsql converts them to UTC
        // instants when writing "timestamp with time zone" columns and back to local time when reading.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Urun> Urunler => Set<Urun>();
    public DbSet<Musteri> Musteriler => Set<Musteri>();
    public DbSet<Siparis> Siparisler => Set<Siparis>();
    public DbSet<SiparisDetay> SiparisDetaylar => Set<SiparisDetay>();
    public DbSet<Fatura> Faturalar => Set<Fatura>();
    public DbSet<Kategori> Kategoriler => Set<Kategori>();
    public DbSet<IslemLog> IslemLoglari => Set<IslemLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp with time zone");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Kategori>(e =>
        {
            e.HasIndex(k => k.Ad).IsUnique();
        });

        modelBuilder.Entity<Urun>(e =>
        {
            e.Property(u => u.Aktif).HasDefaultValue(true);
            e.Property(u => u.Fiyat).HasPrecision(18, 2);
            e.Property(u => u.StokAdedi).IsConcurrencyToken();
            e.HasOne(u => u.Kategori)
                .WithMany(k => k.Urunler)
                .HasForeignKey(u => u.KategoriId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Musteri>(e =>
        {
            e.HasIndex(m => m.Email).IsUnique();
        });

        modelBuilder.Entity<Siparis>(e =>
        {
            e.Property(s => s.Durum)
                .HasConversion<string>()
                .HasMaxLength(20);
            e.Property(s => s.ToplamTutar).HasPrecision(18, 2);
            e.HasIndex(s => s.Durum);
            e.HasIndex(s => s.SiparisTarihi);

            // ERP record integrity: a customer with order history cannot be hard-deleted.
            e.HasOne(s => s.Musteri)
                .WithMany(m => m.Siparisler)
                .HasForeignKey(s => s.MusteriId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SiparisDetay>(e =>
        {
            e.Property(d => d.BirimFiyat).HasPrecision(18, 2);
            e.HasOne(d => d.Siparis)
                .WithMany(s => s.SiparisDetaylar)
                .HasForeignKey(d => d.SiparisId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Urun)
                .WithMany()
                .HasForeignKey(d => d.UrunId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Fatura>(e =>
        {
            e.HasIndex(f => f.FaturaNo).IsUnique();
            e.Property(f => f.ToplamTutar).HasPrecision(18, 2);
            e.HasOne(f => f.Siparis)
                .WithOne(s => s.Fatura)
                .HasForeignKey<Fatura>(f => f.SiparisId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IslemLog>(e =>
        {
            e.HasIndex(l => l.Tarih);
        });
    }
}
