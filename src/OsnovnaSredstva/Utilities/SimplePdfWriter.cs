using System.IO;
using System.Text;

namespace OsnovnaSredstva.Utilities;

public static class SimplePdfWriter
{
    private const double PageWidth = 595d;
    private const double PageHeight = 842d;
    private const double MarginLeft = 40d;
    private const double MarginTop = 802d;
    private const double MarginBottom = 40d;
    private const double LineHeight = 14d;
    private const int FontSize = 10;

    public static void WriteTextPdf(string filePath, string title, IReadOnlyList<string> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var folder = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(folder))
            Directory.CreateDirectory(folder);

        var allLines = new List<string>();
        if (!string.IsNullOrWhiteSpace(title))
        {
            allLines.Add(title.Trim());
            allLines.Add(string.Empty);
        }
        allLines.AddRange(lines);
        if (allLines.Count == 0) allLines.Add("-");

        var rowsPerPage = Math.Max(1, (int)Math.Floor((MarginTop - MarginBottom) / LineHeight));
        var pages = SplitToPages(allLines, rowsPerPage);
        var pageCount = pages.Count;
        var fontObjectNumber = 3 + (pageCount * 2);
        var maxObjectNumber = fontObjectNumber;

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        var offsets = new long[maxObjectNumber + 1];

        WriteAscii(stream, "%PDF-1.4\n%SimplePdfWriter\n");

        for (var n = 1; n <= maxObjectNumber; n++)
        {
            offsets[n] = stream.Position;
            WriteAscii(stream, $"{n} 0 obj\n");
            WriteAscii(stream, BuildObject(n, pages, fontObjectNumber));
            WriteAscii(stream, "\nendobj\n");
        }

        var xrefPosition = stream.Position;
        WriteAscii(stream, $"xref\n0 {maxObjectNumber + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        for (var n = 1; n <= maxObjectNumber; n++)
            WriteAscii(stream, $"{offsets[n]:0000000000} 00000 n \n");

        WriteAscii(stream, $"trailer\n<< /Size {maxObjectNumber + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");
    }

    private static string BuildObject(int n, IReadOnlyList<IReadOnlyList<string>> pages, int fontObjNum)
    {
        if (n == 1) return "<< /Type /Catalog /Pages 2 0 R >>";
        if (n == 2)
        {
            var kids = string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => $"{3 + i * 2} 0 R"));
            return $"<< /Type /Pages /Count {pages.Count} /Kids [{kids}] >>";
        }

        var pageIndex = (n - 3) / 2;
        var isPage = (n - 3) % 2 == 0;

        if (pageIndex >= 0 && pageIndex < pages.Count)
        {
            var pageObjNum = 3 + pageIndex * 2;
            var contentObjNum = pageObjNum + 1;

            if (isPage)
                return $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}] /Resources << /Font << /F1 {fontObjNum} 0 R >> >> /Contents {contentObjNum} 0 R >>";

            var body = BuildContentStream(pages[pageIndex]);
            return $"<< /Length {Encoding.ASCII.GetByteCount(body)} >>\nstream\n{body}\nendstream";
        }

        if (n == fontObjNum)
            return "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";

        throw new InvalidOperationException($"Neočekivan PDF objekat: {n}");
    }

    private static string BuildContentStream(IReadOnlyList<string> lines)
    {
        var sb = new StringBuilder();
        sb.Append($"BT\n/F1 {FontSize} Tf\n{LineHeight:0.##} TL\n{MarginLeft:0.##} {MarginTop:0.##} Td\n");
        for (int i = 0; i < lines.Count; i++)
        {
            sb.Append('(').Append(EscapePdfText(lines[i])).Append(") Tj\n");
            if (i < lines.Count - 1) sb.Append("T*\n");
        }
        sb.Append("ET");
        return sb.ToString();
    }

    private static IReadOnlyList<IReadOnlyList<string>> SplitToPages(IReadOnlyList<string> lines, int rowsPerPage)
    {
        var pages = new List<IReadOnlyList<string>>();
        for (int i = 0; i < lines.Count; i += rowsPerPage)
        {
            var count = Math.Min(rowsPerPage, lines.Count - i);
            pages.Add(lines.Skip(i).Take(count).ToList());
        }
        if (pages.Count == 0) pages.Add(["-"]);
        return pages;
    }

    private static string EscapePdfText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c == '\\') { sb.Append("\\\\"); continue; }
            if (c == '(') { sb.Append("\\("); continue; }
            if (c == ')') { sb.Append("\\)"); continue; }
            if (c is '\r' or '\n') { sb.Append(' '); continue; }
            sb.Append(c < 32 || c > 126 ? '?' : c);
        }
        return sb.ToString();
    }

    private static void WriteAscii(Stream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}
