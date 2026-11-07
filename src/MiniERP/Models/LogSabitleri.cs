namespace MiniERP.Models;

/// <summary>Audit log module names (stored values).</summary>
public static class LogModul
{
    public const string Kullanici = "User";
    public const string Musteri = "Customer";
    public const string Urun = "Product";
    public const string Kategori = "Category";
    public const string Siparis = "Order";
    public const string Fatura = "Invoice";
}

/// <summary>Audit log action names (stored values).</summary>
public static class LogIslem
{
    public const string Olusturma = "Created";
    public const string Guncelleme = "Updated";
    public const string Silme = "Deleted";
    public const string PasifeAlma = "Deactivated";
    public const string DurumGuncelleme = "Status Updated";
    public const string OdemeDurumu = "Payment Status";
}
