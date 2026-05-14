using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services.Dbf;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OsnovnaSredstva.Services;

public class OsAuthService : IAuthService
{
    private readonly IPutanjaService _putanjaService;

    public OsAuthService(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
    }

    public Task<Korisnik?> PrijavaAsync(string korisnikIme, string lozinka)
    {
        return Task.Run(() => Prijava(korisnikIme, lozinka));
    }

    private Korisnik? Prijava(string korisnikIme, string lozinka)
    {
        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja)) return null;

        var lozinkeFajl = DbfDijagnostika.PronadjiLozinkeFajl(finPutanja);
        if (lozinkeFajl is null) return null;

        try
        {
            var reader = new SimpleDbfReader(lozinkeFajl);
            var lozinkaMd5 = Md5Hash(lozinka);
            var lozinkaUpper = lozinka.ToUpperInvariant();

            foreach (var rec in reader.Zapisi())
            {
                var dbfIme = rec.DajString("KOR_IME").Trim();
                if (!string.Equals(dbfIme, korisnikIme.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                var aktivan = rec.DajBool("AKTIVAN");
                if (!aktivan) return null;

                var dbfLozinka = rec.DajString("LOZINKA").Trim();

                // Pokušavamo MD5 hash poređenje, pa plain text
                bool lozinkaOk = string.Equals(dbfLozinka, lozinkaMd5, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(dbfLozinka, lozinka, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(dbfLozinka, lozinkaUpper, StringComparison.OrdinalIgnoreCase)
                               || string.IsNullOrEmpty(dbfLozinka);

                if (!lozinkaOk) return null;

                return new Korisnik
                {
                    Pas = "0",
                    KorisnikIme = rec.DajString("KOR_IME").Trim(),
                    KorisnikIme2 = rec.DajString("KOR_IME2").Trim(),
                    Lozinka = dbfLozinka,
                    Aktivan = aktivan,
                    JeSupervizor = rec.DajBool("SUPERVIZ"),
                    PravaNivo = rec.DajInt("PRAVA_NIV"),
                    PassGk = rec.DajBool("PASS_GK"),
                    PassAn = rec.DajBool("PASS_AN"),
                    PassBl = rec.DajBool("PASS_BL"),
                    PassTv = rec.DajBool("PASS_TV"),
                    PassTm = rec.DajBool("PASS_TM"),
                    PassUs = rec.DajBool("PASS_US"),
                    PassLd = rec.DajBool("PASS_LD"),
                    PassOst = rec.DajBool("PASS_OST"),
                    PassPrn = rec.DajBool("PASS_PRN"),
                    PassPro = rec.DajBool("PASS_PRO"),
                    PassOs = rec.DajBool("PASS_OS"),
                    PassProf = rec.DajBool("PASS_PROF"),
                    PassDel = rec.DajBool("PASS_DEL"),
                };
            }
        }
        catch { }

        return null;
    }

    private static string Md5Hash(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
