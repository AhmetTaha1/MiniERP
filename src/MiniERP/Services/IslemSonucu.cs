namespace MiniERP.Services;

/// <summary>Outcome of a business operation; carries user-facing error messages instead of throwing.</summary>
public sealed record IslemSonucu(bool Basarili, IReadOnlyList<string> Hatalar, int? Id = null)
{
    public static IslemSonucu Ok(int? id = null) => new(true, Array.Empty<string>(), id);
    public static IslemSonucu Hata(params string[] hatalar) => new(false, hatalar);
    public static IslemSonucu Hata(IEnumerable<string> hatalar) => new(false, hatalar.ToList());

    public string IlkHata => Hatalar.FirstOrDefault() ?? string.Empty;
}
