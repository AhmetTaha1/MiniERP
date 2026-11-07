namespace MiniERP.Entities;

public class Fatura
{
    public int Id { get; set; }
    public int SiparisId { get; set; }
    public Siparis Siparis { get; set; } = null!;
    public string FaturaNo { get; set; } = string.Empty;
    public DateTime FaturaTarihi { get; set; } = DateTime.Now;
    public decimal ToplamTutar { get; set; }
    public bool OdenmeDurumu { get; set; } = false;
}