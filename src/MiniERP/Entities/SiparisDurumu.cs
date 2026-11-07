namespace MiniERP.Entities;

/// <summary>
/// Order lifecycle. Allowed transitions are defined in <see cref="MiniERP.Services.SiparisDurumKurallari"/>.
/// Stored as text in the database.
/// </summary>
public enum SiparisDurumu
{
    Bekliyor,
    Hazirlaniyor,
    Tamamlandi,
    Iptal
}
