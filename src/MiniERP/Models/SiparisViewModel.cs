namespace MiniERP.Models;

public class SiparisViewModel
{
    public int MusteriId { get; set; }
    public List<SiparisKalemi> Kalemler { get; set; } = new();
}

public class SiparisKalemi
{
    public int UrunId { get; set; }
    public int Miktar { get; set; }
}
