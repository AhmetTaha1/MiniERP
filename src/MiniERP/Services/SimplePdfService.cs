using System.Text;

namespace MiniERP.Services;

public static class SimplePdfService
{
    public static byte[] Create(string title, IEnumerable<string> lines)
    {
        var allLines = new List<string> { title, string.Empty };
        allLines.AddRange(lines);

        var pages = allLines
            .Select(Normalize)
            .Chunk(42)
            .ToList();

        if (pages.Count == 0)
        {
            pages.Add([Normalize(title)]);
        }

        var objects = new List<string> { string.Empty, string.Empty, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
        var pageObjectIds = new List<int>();

        foreach (var pageLines in pages)
        {
            var contentObjectId = objects.Count + 1;
            var pageObjectId = objects.Count + 2;
            var stream = BuildPageStream(pageLines);

            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectId} 0 R >>");
            pageObjectIds.Add(pageObjectId);
        }

        objects[0] = "<< /Type /Catalog /Pages 2 0 R >>";
        objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageObjectIds.Count} >>";

        var sb = new StringBuilder();
        var offsets = new List<int> { 0 };
        sb.Append("%PDF-1.4\n");

        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append(i + 1).Append(" 0 obj\n");
            sb.Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append("xref\n");
        sb.Append("0 ").Append(objects.Count + 1).Append('\n');
        sb.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            sb.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        sb.Append("trailer\n");
        sb.Append("<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        sb.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string BuildPageStream(IEnumerable<string> lines)
    {
        var sb = new StringBuilder();
        sb.Append("BT\n");
        sb.Append("/F1 16 Tf\n");
        sb.Append("50 790 Td\n");

        var first = true;
        foreach (var line in lines)
        {
            if (!first)
            {
                sb.Append("0 -17 Td\n");
            }

            sb.Append('(').Append(Escape(line)).Append(") Tj\n");
            if (first)
            {
                sb.Append("/F1 10 Tf\n");
            }

            first = false;
        }

        sb.Append("ET");
        return sb.ToString();
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private static string Normalize(string value)
    {
        return value
            .Replace('ç', 'c').Replace('Ç', 'C')
            .Replace('ğ', 'g').Replace('Ğ', 'G')
            .Replace('ı', 'i').Replace('İ', 'I')
            .Replace('ö', 'o').Replace('Ö', 'O')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ü', 'u').Replace('Ü', 'U')
            .Replace("₺", "TRY ");
    }
}
