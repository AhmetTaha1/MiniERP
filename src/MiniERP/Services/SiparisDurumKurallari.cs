using MiniERP.Entities;

namespace MiniERP.Services;

/// <summary>
/// Pure order state-machine rules:
/// Pending → Preparing | Canceled, Preparing → Completed | Canceled. Completed and Canceled are final.
/// </summary>
public static class SiparisDurumKurallari
{
    private static readonly IReadOnlyDictionary<SiparisDurumu, SiparisDurumu[]> Gecisler =
        new Dictionary<SiparisDurumu, SiparisDurumu[]>
        {
            [SiparisDurumu.Bekliyor] = [SiparisDurumu.Hazirlaniyor, SiparisDurumu.Iptal],
            [SiparisDurumu.Hazirlaniyor] = [SiparisDurumu.Tamamlandi, SiparisDurumu.Iptal],
            [SiparisDurumu.Tamamlandi] = [],
            [SiparisDurumu.Iptal] = []
        };

    public static bool GecisGecerliMi(SiparisDurumu mevcut, SiparisDurumu yeni) =>
        Gecisler.TryGetValue(mevcut, out var izinli) && izinli.Contains(yeni);

    public static IReadOnlyList<SiparisDurumu> IzinliGecisler(SiparisDurumu mevcut) =>
        Gecisler.TryGetValue(mevcut, out var izinli) ? izinli : [];

    public static bool SonDurumMu(SiparisDurumu durum) => IzinliGecisler(durum).Count == 0;

    /// <summary>Stock is reserved while an order is active and released when it is canceled.</summary>
    public static bool StokIadesiGerekirMi(SiparisDurumu mevcut, SiparisDurumu yeni) =>
        yeni == SiparisDurumu.Iptal && mevcut != SiparisDurumu.Iptal;

    /// <summary>Products referenced by an active order cannot be deactivated.</summary>
    public static readonly SiparisDurumu[] AktifDurumlar = [SiparisDurumu.Bekliyor, SiparisDurumu.Hazirlaniyor];
}
