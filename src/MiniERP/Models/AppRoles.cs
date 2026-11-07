namespace MiniERP.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Satis = "Satis";
    public const string Stok = "Stok";
    public const string Muhasebe = "Muhasebe";
    public const string Yonetici = "Yonetici";

    public static readonly string[] All = [Admin, Satis, Stok, Muhasebe, Yonetici];

    public const string BirimSatis = "Satış";
    public const string BirimStok = "Stok / Üretim";
    public const string BirimMuhasebe = "Muhasebe";
    public const string BirimYonetici = "Yönetim";

    public static readonly string[] YetkiBirimleri = [BirimSatis, BirimStok, BirimMuhasebe, BirimYonetici];

    public static string? GetRoleForBirim(string birim)
    {
        return birim switch
        {
            BirimSatis => Satis,
            BirimStok => Stok,
            BirimMuhasebe => Muhasebe,
            BirimYonetici => Yonetici,
            _ => null
        };
    }

    public static string GetDisplayNameForRole(string role)
    {
        return role switch
        {
            Admin => "System Admin",
            Satis => "Sales",
            Stok => "Stock / Production",
            Muhasebe => "Accounting",
            Yonetici => "Management",
            _ => role
        };
    }
}
