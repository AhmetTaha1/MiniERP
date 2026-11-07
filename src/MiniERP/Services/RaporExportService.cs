using ClosedXML.Excel;
using MiniERP.Models;

namespace MiniERP.Services;

/// <summary>Exports the management report as a native .xlsx workbook or a PDF summary.</summary>
public static class RaporExportService
{
    private const string ParaFormati = "₺#,##0.00";

    public static byte[] Excel(RaporViewModel model)
    {
        using var workbook = new XLWorkbook();

        var ozet = workbook.Worksheets.Add("Summary");
        ozet.Cell(1, 1).Value = "MiniERP Management Report";
        ozet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        ozet.Cell(2, 1).Value = model.FiltreMetni;
        ozet.Cell(4, 1).Value = "Metric";
        ozet.Cell(4, 2).Value = "Value";
        BaslikStili(ozet.Range(4, 1, 4, 2));
        ozet.Cell(5, 1).Value = "Total Collections";
        ozet.Cell(5, 2).Value = model.ToplamGelir;
        ozet.Cell(5, 2).Style.NumberFormat.Format = ParaFormati;
        ozet.Cell(6, 1).Value = "Pending Orders";
        ozet.Cell(6, 2).Value = model.BekleyenSiparisSayisi;
        ozet.Cell(7, 1).Value = "Critical Stock Items";
        ozet.Cell(7, 2).Value = model.KritikStokSayisi;
        ozet.Cell(8, 1).Value = "Unpaid Invoices";
        ozet.Cell(8, 2).Value = model.OdenmemisFaturaSayisi;
        ozet.Cell(9, 1).Value = "Unpaid Invoice Amount";
        ozet.Cell(9, 2).Value = model.OdenmemisFaturaTutari;
        ozet.Cell(9, 2).Style.NumberFormat.Format = ParaFormati;
        ozet.Columns().AdjustToContents();

        TabloEkle(workbook, "Sales by Category",
            ["Category", "Product", "Units Sold", "Amount", "Current Stock"],
            model.KategoriUrunRaporu.Select(x => new object[] { x.Kategori, x.UrunAdi, x.SatilanAdet, x.Tutar, x.MevcutStok }),
            paraKolonu: 4);

        TabloEkle(workbook, "Sales by Customer",
            ["Customer", "Order Count", "Total Amount"],
            model.MusteriSatisRaporu.Select(x => new object[] { x.Musteri, x.SiparisSayisi, x.ToplamTutar }),
            paraKolonu: 3);

        TabloEkle(workbook, "Critical Stock",
            ["Product", "Category", "Stock"],
            model.KritikStoklar.Select(x => new object[] { x.UrunAdi, x.Kategori, x.StokAdedi }));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] Pdf(RaporViewModel model)
    {
        var satirlar = new List<string>
        {
            model.FiltreMetni,
            $"Total collections: {DisplayText.Money(model.ToplamGelir)}",
            $"Pending orders: {model.BekleyenSiparisSayisi}",
            $"Critical stock: {model.KritikStokSayisi}",
            $"Unpaid invoice amount: {DisplayText.Money(model.OdenmemisFaturaTutari)}",
            string.Empty,
            "Customer-based sales:"
        };
        satirlar.AddRange(model.MusteriSatisRaporu.Take(12)
            .Select(m => $"- {m.Musteri}: {m.SiparisSayisi} orders, {DisplayText.Money(m.ToplamTutar)}"));
        satirlar.Add(string.Empty);
        satirlar.Add("Product sales by category:");
        satirlar.AddRange(model.KategoriUrunRaporu.Take(18)
            .Select(u => $"- {u.Kategori} / {u.UrunAdi}: {u.SatilanAdet} units, {DisplayText.Money(u.Tutar)}"));

        return SimplePdfService.Create("MiniERP Management Report", satirlar);
    }

    private static void TabloEkle(XLWorkbook workbook, string ad, string[] basliklar, IEnumerable<object[]> satirlar, int? paraKolonu = null)
    {
        var sheet = workbook.Worksheets.Add(ad);
        for (var i = 0; i < basliklar.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = basliklar[i];
        }
        BaslikStili(sheet.Range(1, 1, 1, basliklar.Length));

        var satir = 2;
        foreach (var degerler in satirlar)
        {
            for (var i = 0; i < degerler.Length; i++)
            {
                sheet.Cell(satir, i + 1).Value = XLCellValue.FromObject(degerler[i]);
            }
            satir++;
        }

        if (paraKolonu.HasValue)
        {
            sheet.Column(paraKolonu.Value).Style.NumberFormat.Format = ParaFormati;
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
    }

    private static void BaslikStili(IXLRange range)
    {
        range.Style.Font.SetBold();
        range.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#2d3561"));
        range.Style.Font.SetFontColor(XLColor.White);
    }
}
