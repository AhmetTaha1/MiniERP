using System.Text;
using MiniERP.Models;
using MiniERP.Services;

namespace MiniERP.Tests;

public class SimplePdfServiceTests
{
    [Fact]
    public void Creates_a_structurally_valid_pdf()
    {
        var pdf = Encoding.ASCII.GetString(SimplePdfService.Create("Title", ["line 1", "line (2)"]));

        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.EndsWith("%%EOF", pdf);
        Assert.Contains("(line \\(2\\)) Tj", pdf);
    }

    [Fact]
    public void Turkish_characters_and_currency_are_transliterated_for_the_base_font()
    {
        var pdf = Encoding.ASCII.GetString(SimplePdfService.Create("Şirket Özeti", ["Çağrı Ünlü: ₺1,250.00"]));

        Assert.Contains("(Sirket Ozeti) Tj", pdf);
        Assert.Contains("(Cagri Unlu: TRY 1,250.00) Tj", pdf);
    }

    [Fact]
    public void Long_documents_are_split_into_pages()
    {
        var lines = Enumerable.Range(1, 100).Select(i => $"Line {i}");

        var pdf = Encoding.ASCII.GetString(SimplePdfService.Create("Long", lines));

        Assert.Contains("/Count 3", pdf);
    }

    [Theory]
    [InlineData(1234.5, "₺1,234.50")]
    [InlineData(0, "₺0.00")]
    [InlineData(-10, "-₺10.00")]
    public void Money_is_formatted_in_lira(decimal value, string expected)
    {
        Assert.Equal(expected, DisplayText.Money(value));
    }
}
