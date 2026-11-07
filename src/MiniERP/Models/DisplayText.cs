using System.Globalization;
using MiniERP.Entities;

namespace MiniERP.Models;

/// <summary>UI display helpers. Older audit-log rows were stored in Turkish, so both forms are mapped.</summary>
public static class DisplayText
{
    private static readonly CultureInfo ParaKulturu = CreateMoneyCulture();

    public static string Money(decimal value) => value.ToString("C2", ParaKulturu);

    public static string RoleUnit(string value) => value switch
    {
        AppRoles.BirimSatis => "Sales",
        AppRoles.BirimStok => "Stock / Production",
        AppRoles.BirimMuhasebe => "Accounting",
        AppRoles.BirimYonetici => "Management",
        _ => value
    };

    public static string OrderStatus(SiparisDurumu value) => value switch
    {
        SiparisDurumu.Bekliyor => "Pending",
        SiparisDurumu.Hazirlaniyor => "Preparing",
        SiparisDurumu.Tamamlandi => "Completed",
        SiparisDurumu.Iptal => "Canceled",
        _ => value.ToString()
    };

    public static string OrderStatusBadge(SiparisDurumu value) => value switch
    {
        SiparisDurumu.Bekliyor => "badge-warning",
        SiparisDurumu.Hazirlaniyor => "badge-info",
        SiparisDurumu.Tamamlandi => "badge-success",
        SiparisDurumu.Iptal => "badge-danger",
        _ => "badge-secondary"
    };

    public static string AuditModule(string value) => value switch
    {
        "Kullanıcı" => LogModul.Kullanici,
        "Müşteri" => LogModul.Musteri,
        "Ürün" => LogModul.Urun,
        "Kategori" => LogModul.Kategori,
        "Sipariş" => LogModul.Siparis,
        "Fatura" => LogModul.Fatura,
        _ => value
    };

    public static string AuditAction(string value) => value switch
    {
        "Oluşturma" => LogIslem.Olusturma,
        "Güncelleme" => LogIslem.Guncelleme,
        "Silme" => LogIslem.Silme,
        "Pasife Alma" => LogIslem.PasifeAlma,
        "Durum Güncelleme" => LogIslem.DurumGuncelleme,
        "Ödeme Durumu" => LogIslem.OdemeDurumu,
        _ => value
    };

    private static CultureInfo CreateMoneyCulture()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        culture.NumberFormat.CurrencySymbol = "₺";
        culture.NumberFormat.CurrencyNegativePattern = 1; // -₺n
        return culture;
    }
}
