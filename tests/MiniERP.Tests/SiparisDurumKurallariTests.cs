using MiniERP.Entities;
using MiniERP.Services;

namespace MiniERP.Tests;

public class SiparisDurumKurallariTests
{
    [Theory]
    [InlineData(SiparisDurumu.Bekliyor, SiparisDurumu.Hazirlaniyor, true)]
    [InlineData(SiparisDurumu.Bekliyor, SiparisDurumu.Iptal, true)]
    [InlineData(SiparisDurumu.Bekliyor, SiparisDurumu.Tamamlandi, false)]
    [InlineData(SiparisDurumu.Hazirlaniyor, SiparisDurumu.Tamamlandi, true)]
    [InlineData(SiparisDurumu.Hazirlaniyor, SiparisDurumu.Iptal, true)]
    [InlineData(SiparisDurumu.Hazirlaniyor, SiparisDurumu.Bekliyor, false)]
    [InlineData(SiparisDurumu.Tamamlandi, SiparisDurumu.Iptal, false)]
    [InlineData(SiparisDurumu.Iptal, SiparisDurumu.Bekliyor, false)]
    public void GecisGecerliMi_follows_the_order_state_machine(SiparisDurumu mevcut, SiparisDurumu yeni, bool beklenen)
    {
        Assert.Equal(beklenen, SiparisDurumKurallari.GecisGecerliMi(mevcut, yeni));
    }

    [Theory]
    [InlineData(SiparisDurumu.Tamamlandi)]
    [InlineData(SiparisDurumu.Iptal)]
    public void Completed_and_canceled_are_final_states(SiparisDurumu durum)
    {
        Assert.True(SiparisDurumKurallari.SonDurumMu(durum));
        Assert.Empty(SiparisDurumKurallari.IzinliGecisler(durum));
    }

    [Theory]
    [InlineData(SiparisDurumu.Bekliyor, SiparisDurumu.Iptal, true)]
    [InlineData(SiparisDurumu.Hazirlaniyor, SiparisDurumu.Iptal, true)]
    [InlineData(SiparisDurumu.Bekliyor, SiparisDurumu.Hazirlaniyor, false)]
    [InlineData(SiparisDurumu.Hazirlaniyor, SiparisDurumu.Tamamlandi, false)]
    public void Stock_is_returned_only_when_an_order_is_canceled(SiparisDurumu mevcut, SiparisDurumu yeni, bool beklenen)
    {
        Assert.Equal(beklenen, SiparisDurumKurallari.StokIadesiGerekirMi(mevcut, yeni));
    }
}
