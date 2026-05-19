using System.IO;
using System.Text;

namespace OsnovnaSredstva.Services.Dbf;

/// <summary>
/// Minimalni reader za dBASE/VFP DBF fajlove.
/// Podržava tipove: C (Character), N (Numeric), L (Logical), D (Date).
/// </summary>
public static class DbfReader
{
    public static List<Dictionary<string, object?>> CitajSveZapise(string putanjaFajla)
    {
        if (!File.Exists(putanjaFajla))
            throw new FileNotFoundException($"DBF fajl nije pronađen: {putanjaFajla}");

        using var stream = new FileStream(putanjaFajla, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream, Encoding.ASCII);

        reader.ReadByte();                          // verzija
        reader.ReadBytes(3);                        // datum izmene
        int brojZapisa = (int)reader.ReadUInt32();  // broj zapisa
        int velicinaHeadera = reader.ReadUInt16();  // veličina headera
        int velicinaZapisa = reader.ReadUInt16();   // veličina zapisa
        reader.ReadBytes(16);                       // rezervisano (bytes 12-27)
        reader.ReadByte();                          // byte 28: table flags
        byte kodStranice = reader.ReadByte();       // byte 29: code page mark
        reader.ReadBytes(2);                        // bytes 30-31: rezervisano

        var polja = new List<DbfPolje>();
        while (true)
        {
            byte prvi = reader.ReadByte();
            if (prvi == 0x0D) break;

            var imeBytes = new byte[11];
            imeBytes[0] = prvi;
            reader.Read(imeBytes, 1, 10);

            string ime = Encoding.ASCII.GetString(imeBytes).TrimEnd('\0');
            char tip = (char)reader.ReadByte();
            reader.ReadBytes(4);
            int duzina = reader.ReadByte();
            int decimale = reader.ReadByte();
            reader.ReadBytes(14);

            polja.Add(new DbfPolje(ime, tip, duzina, decimale));
        }

        stream.Seek(velicinaHeadera, SeekOrigin.Begin);

        var encoding = MapirajEncoding(kodStranice);
        var rezultati = new List<Dictionary<string, object?>>();

        for (int i = 0; i < brojZapisa; i++)
        {
            byte flagBrisanja = reader.ReadByte();

            if (flagBrisanja == '*')
            {
                reader.ReadBytes(velicinaZapisa - 1);
                continue;
            }

            var zapis = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var polje in polja)
            {
                byte[] siroviPodaci = reader.ReadBytes(polje.Duzina);
                string tekst = encoding.GetString(siroviPodaci).Trim();

                zapis[polje.Ime] = polje.Tip switch
                {
                    'L' => tekst.ToUpperInvariant() is "T" or "Y" or ".T.",
                    'N' => decimal.TryParse(tekst, out var n) ? n : (decimal?)null,
                    'D' => tekst.Length == 8 && DateTime.TryParseExact(tekst, "yyyyMMdd",
                               null, System.Globalization.DateTimeStyles.None, out var d) ? d : (DateTime?)null,
                    _ => tekst
                };
            }

            rezultati.Add(zapis);
        }

        return rezultati;
    }

    public static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) ? Convert.ToString(v)?.Trim() ?? string.Empty : string.Empty;

    public static bool Bool(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return false;
        return v switch
        {
            bool b => b,
            decimal d => d != 0m,
            int i => i != 0,
            string s => s.Trim().ToUpperInvariant() is "T" or "Y" or ".T." or "1" or "D" or "TRUE",
            _ => false
        };
    }

    public static decimal Dec(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0m;
        return v switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            _ => decimal.TryParse(Convert.ToString(v), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m
        };
    }

    public static long Long(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v is null) return 0L;
        return v switch
        {
            long l => l,
            int i => i,
            decimal d => (long)d,
            _ => long.TryParse(Convert.ToString(v), out var parsed) ? parsed : 0L
        };
    }

    public static bool JeAktivan(Dictionary<string, object?> zapis)
    {
        if (!zapis.TryGetValue("AKTIVAN", out var value) || value is null)
            return true;

        if (value is string s)
        {
            var trimmed = s.Trim();
            if (string.IsNullOrEmpty(trimmed)) return true;
            return trimmed.ToUpperInvariant() is "T" or "Y" or ".T." or "1" or "D" or "TRUE";
        }

        return value switch
        {
            bool b => b,
            decimal d => d != 0m,
            int i => i != 0,
            _ => false
        };
    }

    private record DbfPolje(string Ime, char Tip, int Duzina, int Decimale);

    private static Encoding MapirajEncoding(byte kod) => kod switch
    {
        0x01 => Encoding.GetEncoding(437),
        0x02 => Encoding.GetEncoding(850),
        0x03 => Encoding.GetEncoding(1252),
        0x64 => Encoding.GetEncoding(852),
        0x65 => Encoding.GetEncoding(866),
        0xC8 => Encoding.GetEncoding(1250),
        0xC9 => Encoding.GetEncoding(1251),
        0xCB => Encoding.GetEncoding(1253),
        _ => Encoding.GetEncoding(1250),
    };
}
