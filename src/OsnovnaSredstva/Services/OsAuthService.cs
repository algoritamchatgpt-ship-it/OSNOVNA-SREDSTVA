using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services.Dbf;
using Serilog;

namespace OsnovnaSredstva.Services;

public class OsAuthService : IAuthService
{
    private readonly IPutanjaService _putanjaService;

    public OsAuthService(IPutanjaService putanjaService)
    {
        _putanjaService = putanjaService;
    }

    public Task<PrijavaRezultat> PrijavaAsync(string korisnikIme, string lozinka)
    {
        return Task.Run(() => Prijava(korisnikIme, lozinka));
    }

    private PrijavaRezultat Prijava(string korisnikIme, string lozinka)
    {
        var finPutanja = _putanjaService.DajFinPutanju();
        if (string.IsNullOrWhiteSpace(finPutanja))
            return Greska("FIN putanja nije postavljena. Idite na početni ekran i podesite putanju.");

        FinWorkspaceResolver.EnsureLozinkeTables(finPutanja, out _, out _, out _);

        var lozinkeFajl = FinWorkspaceResolver.ResolveLozinkeTablePath(finPutanja);
        if (string.IsNullOrWhiteSpace(lozinkeFajl))
            return Greska($"LOZINKE.DBF nije pronađen.\nFIN putanja: {finPutanja}");

        Log.Debug("Prijava: čitam {Fajl}", lozinkeFajl);

        try
        {
            var zapisi = DbfReader.CitajSveZapise(lozinkeFajl);
            var zapis = zapisi.FirstOrDefault(z =>
                string.Equals(DbfReader.Str(z, "KORISNIK"), korisnikIme, StringComparison.OrdinalIgnoreCase));

            if (zapis is null)
            {
                Log.Warning("Prijava fail: korisnik '{K}' nije pronađen u {F} ({N} zapisa)",
                    korisnikIme, lozinkeFajl, zapisi.Count);
                return Greska($"Korisnik '{korisnikIme}' nije pronađen.\n(ukupno korisnika u bazi: {zapisi.Count})");
            }

            if (!DbfReader.JeAktivan(zapis))
            {
                Log.Warning("Prijava fail: korisnik '{K}' nije aktivan.", korisnikIme);
                return Greska($"Korisnik '{korisnikIme}' nije aktivan u sistemu.");
            }

            var dbfLozinka = DbfReader.Str(zapis, "LOZINKA");
            if (!string.IsNullOrEmpty(dbfLozinka) &&
                !string.Equals(dbfLozinka, lozinka, StringComparison.Ordinal) &&
                !string.Equals(dbfLozinka, lozinka, StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("Prijava fail: lozinka nije ispravna za '{K}'.", korisnikIme);
                return Greska("Lozinka nije ispravna.");
            }

            var pravaNivo = (int)(DbfReader.Dec(zapis, "PASSNIVO"));

            return new PrijavaRezultat(new Korisnik
            {
                Pas = DbfReader.Str(zapis, "PAS"),
                KorisnikIme = DbfReader.Str(zapis, "KORISNIK"),
                KorisnikIme2 = DbfReader.Str(zapis, "KORIME"),
                Lozinka = dbfLozinka,
                Aktivan = true,
                JeSupervizor = DbfReader.Str(zapis, "OPERATER") == "1" || pravaNivo > 0,
                PravaNivo = pravaNivo,
                PassGk = DbfReader.Bool(zapis, "PASSGK"),
                PassAn = DbfReader.Bool(zapis, "PASSAN"),
                PassBl = DbfReader.Bool(zapis, "PASSBL"),
                PassTv = DbfReader.Bool(zapis, "PASSTV"),
                PassTm = DbfReader.Bool(zapis, "PASSTM"),
                PassUs = DbfReader.Bool(zapis, "PASSUS"),
                PassLd = DbfReader.Bool(zapis, "PASSLD"),
                PassOst = DbfReader.Bool(zapis, "PASSOST"),
                PassPrn = DbfReader.Bool(zapis, "PASSPRN"),
                PassPro = DbfReader.Bool(zapis, "PASSPRO"),
                PassOs = DbfReader.Bool(zapis, "PASSOS"),
                PassProf = DbfReader.Bool(zapis, "PASSPROF"),
                PassDel = DbfReader.Bool(zapis, "PASSDEL"),
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Prijava: greška pri čitanju {Fajl}", lozinkeFajl);
            return Greska($"Greška pri čitanju baze korisnika:\n{ex.Message}");
        }
    }

    private static PrijavaRezultat Greska(string poruka) => new(null, poruka);
}
