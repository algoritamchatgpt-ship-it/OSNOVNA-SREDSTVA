using OsnovnaSredstva.Models;

namespace OsnovnaSredstva.Services;

public class AppState
{
    private IDisposable? _aktivnaSesijaLock;

    public Korisnik? TrenutniKorisnik { get; private set; }
    public Firma? AktivnaFirma { get; private set; }
    public int AktivnaGodina { get; set; } = DateTime.Now.Year;

    public bool JePrijavljen => TrenutniKorisnik != null;
    public bool JeSupervizor => TrenutniKorisnik?.JeSupervizor ?? false;
    public bool ImaFirmu => AktivnaFirma != null;

    public void Prijavi(Korisnik korisnik) => TrenutniKorisnik = korisnik;

    public void PostaviSesijaLock(IDisposable? handle)
    {
        OslobodiSesijaLock();
        _aktivnaSesijaLock = handle;
    }

    public void OslobodiSesijaLock()
    {
        if (_aktivnaSesijaLock is null) return;
        try { _aktivnaSesijaLock.Dispose(); }
        catch { }
        finally { _aktivnaSesijaLock = null; }
    }

    public void Odjavi()
    {
        OslobodiSesijaLock();
        TrenutniKorisnik = null;
        AktivnaFirma = null;
    }

    public void PostaviFirmu(Firma firma) => AktivnaFirma = firma;
}
