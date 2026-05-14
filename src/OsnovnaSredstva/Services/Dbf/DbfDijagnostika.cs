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
        var data00 = Path.Combine(finPutanja, "data00");
        if (!Directory.Exists(data00)) return null;

        foreach (var ime in new[] { "LOZINKE.DBF", "LOZINKEA.DBF", "lozinke.dbf", "lozinkea.dbf" })
        {
            var putanja = Path.Combine(data00, ime);
            if (File.Exists(putanja)) return putanja;
        }

        return null;
    }
}
