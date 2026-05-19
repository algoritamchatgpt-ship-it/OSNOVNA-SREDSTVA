using OsnovnaSredstva.Services.Dbf;
using System.Globalization;
using System.IO;

namespace OsnovnaSredstva.ViewModels;

internal static class PartneriDbfSupport
{
    public static List<PartnerStavka> UcitajPartnere(string folderPath)
    {
        var dbfPath = PronadjiDbf(folderPath, "an0.dbf");
        if (dbfPath == null) return [];

        var zapisi = DbfReader.CitajSveZapise(dbfPath);
        return zapisi.Select(z =>
        {
            var stavka = new PartnerStavka
            {
                Sifra = Str(z, "SIFRA"),
                Naziv = Str(z, "NAZIV"),
                Naziv2 = Str(z, "NAZIV2"),
                Posta = Str(z, "POSTA"),
                Mesto = Str(z, "MESTO"),
                Ulica = Str(z, "ULICA"),
                Ulbroj = Str(z, "ULBROJ"),
                Telefon = Str(z, "TELEFON"),
                Telefon2 = Str(z, "TELEFON2"),
                Fax = Str(z, "FAX"),
                Email = Str(z, "EMAIL"),
                Lice1 = Str(z, "LICE1"),
                TelLice1 = Str(z, "TELLICE1"),
                Pib = Str(z, "PIB"),
                Pib2 = Str(z, "PIB2"),
                Maticni = Str(z, "MATICNI"),
                ZiroRac = Str(z, "ZIRORAC"),
                Drzava = Str(z, "DRZAVA"),
                Preneto = Str(z, "PRENETO"),
                IdBr = Long(z, "IDBR")
            };
            stavka.SetOriginalValues(z);
            return stavka;
        }).ToList();
    }

    public static void SacuvajPartnere(string folderPath, IReadOnlyCollection<PartnerStavka> stavke)
    {
        var targetPath = PronadjiDbf(folderPath, "an0.dbf") ?? Path.Combine(folderPath, "an0.dbf");
        if (!File.Exists(targetPath))
            throw new FileNotFoundException($"Tabela an0.dbf nije pronađena u: {folderPath}");

        var schema = DbfTableWriter.LoadSchema(targetPath);
        DbfTableWriter.WriteTable(targetPath, schema, stavke.ToList(), ResolvePartnerValue);
    }

    public static string? PronadjiDbf(string folderPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return null;

        var direct = Path.Combine(folderPath, fileName);
        if (File.Exists(direct)) return direct;

        return Directory.GetFiles(folderPath, "*.dbf", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(f => string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static object? ResolvePartnerValue(PartnerStavka row, string fieldName) => fieldName.ToUpperInvariant() switch
    {
        "SIFRA" => row.Sifra.Trim(),
        "NAZIV" => row.Naziv.Trim(),
        "NAZIV2" => row.Naziv2.Trim(),
        "POSTA" => row.Posta.Trim(),
        "MESTO" => row.Mesto.Trim(),
        "ULICA" => row.Ulica.Trim(),
        "ULBROJ" => row.Ulbroj.Trim(),
        "TELEFON" => row.Telefon.Trim(),
        "TELEFON2" => row.Telefon2.Trim(),
        "FAX" => row.Fax.Trim(),
        "EMAIL" => row.Email.Trim(),
        "LICE1" => row.Lice1.Trim(),
        "TELLICE1" => row.TelLice1.Trim(),
        "PIB" => row.Pib.Trim(),
        "PIB2" => (string.IsNullOrWhiteSpace(row.Pib2) ? row.Sifra : row.Pib2).Trim(),
        "MATICNI" => row.Maticni.Trim(),
        "ZIRORAC" => row.ZiroRac.Trim(),
        "DRZAVA" => row.Drzava.Trim(),
        "PRENETO" => string.IsNullOrWhiteSpace(row.Preneto) ? " " : row.Preneto.Trim(),
        "IDBR" => row.IdBr,
        _ => row.TryGetOriginalValue(fieldName, out var original) ? original : null
    };

    private static string Str(Dictionary<string, object?> r, string k)
        => r.TryGetValue(k, out var v) && v is string s ? s.Trim() : string.Empty;

    private static long Long(Dictionary<string, object?> r, string k)
    {
        if (!r.TryGetValue(k, out var v) || v == null) return 0L;
        if (v is long l) return l;
        if (v is int i) return i;
        if (v is decimal d) return (long)d;
        return long.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), out var parsed) ? parsed : 0L;
    }
}
