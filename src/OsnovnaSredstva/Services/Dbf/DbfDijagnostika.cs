using System.IO;
using System.Text;

namespace OsnovnaSredstva.Services.Dbf;

public static class DbfDijagnostika
{
    public static string AnalizirajFajl(string filePath)
    {
        try
        {
            var reader = new SimpleDbfReader(filePath);
            var sb = new StringBuilder();
            sb.AppendLine($"Fajl: {Path.GetFileName(filePath)}");
            sb.AppendLine($"Zapisa: {reader.RecordCount}");
            sb.AppendLine($"Polja ({reader.Fields.Count}):");
            foreach (var f in reader.Fields)
                sb.AppendLine($"  {f.Name,-15} {f.Type}({f.Length},{f.Decimals})");

            sb.AppendLine();
            int n = 0;
            foreach (var rec in reader.Zapisi())
            {
                if (n >= 5) break;
                sb.Append($"  Zapis {n + 1}: ");
                foreach (var f in reader.Fields)
                    sb.Append($"{f.Name}={rec.DajString(f.Name)} | ");
                sb.AppendLine();
                n++;
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Greška pri čitanju DBF fajla:\n{ex.Message}";
        }
    }

    public static string? PronadjiLozinkeFajl(string finPutanja)
    {
        if (!Directory.Exists(finPutanja)) return null;

        var kandidati = new[] { "LOZINKE.DBF", "LOZINKEA.DBF", "lozinke.dbf", "lozinkea.dbf" };

        // 1. Root data00 (standardna FIN lokacija)
        var data00 = Path.Combine(finPutanja, "data00");
        if (Directory.Exists(data00))
        {
            foreach (var ime in kandidati)
            {
                var p = Path.Combine(data00, ime);
                if (File.Exists(p)) return p;
            }
        }

        // 2. Direktno u root folderu
        foreach (var ime in kandidati)
        {
            var p = Path.Combine(finPutanja, ime);
            if (File.Exists(p)) return p;
        }

        // 3. U data00 prve firme (F1, F2, ...)
        var folderiFirmi = Directory.GetDirectories(finPutanja)
            .Where(d => System.Text.RegularExpressions.Regex.IsMatch(
                Path.GetFileName(d), @"^F\d+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase);

        foreach (var firmaFolder in folderiFirmi)
        {
            var firmaData00 = Path.Combine(firmaFolder, "data00");
            if (!Directory.Exists(firmaData00)) continue;
            foreach (var ime in kandidati)
            {
                var p = Path.Combine(firmaData00, ime);
                if (File.Exists(p)) return p;
            }
        }

        return null;
    }
}
