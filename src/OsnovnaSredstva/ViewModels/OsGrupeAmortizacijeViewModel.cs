using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsnovnaSredstva.Models;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.Services.Dbf;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;

namespace OsnovnaSredstva.ViewModels;

public partial class OsGrupeAmortizacijeViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext<OsGrupeAmortizacijeViewModel>();
    private readonly AppState _appState;

    [ObservableProperty] private ObservableCollection<OsAgPodStavka> _grupe  = [];
    [ObservableProperty] private OsAgPodStavka?                      _izabranaGrupa;
    [ObservableProperty] private string                              _poruka = "";

    public OsGrupeAmortizacijeViewModel(AppState appState)
    {
        _appState = appState;
        Ucitaj();
    }

    private void Ucitaj()
    {
        var path = DbfPutanja("osagpod.dbf");
        if (path == null) { Grupe = []; Poruka = "osagpod.dbf nije pronađen."; return; }
        try
        {
            Grupe = new ObservableCollection<OsAgPodStavka>(
                DbfReader.CitajSveZapise(path).Select(r => new OsAgPodStavka
                {
                    AgPod   = DbfReader.Str(r, "AGPOD"),
                    Ag      = DbfReader.Str(r, "AG"),
                    Opis    = DbfReader.Str(r, "OPIS"),
                    Preneto = DbfReader.Str(r, "PRENETO"),
                    IDBr    = (int)DbfReader.Dec(r, "IDBR"),
                }));
            Poruka = $"Učitano {Grupe.Count} grupa za amortizaciju.";
            _log.Debug("osagpod.dbf: učitano {Count} grupa", Grupe.Count);
        }
        catch (Exception ex) { Grupe = []; Poruka = $"Greška pri čitanju: {ex.Message}"; _log.Error(ex, "Greška pri čitanju osagpod.dbf"); }
    }

    [RelayCommand]
    private void Sacuvaj()
    {
        var path = DbfPutanja("osagpod.dbf");
        if (path == null) { Poruka = "osagpod.dbf nije pronađen."; return; }
        try
        {
            var schema = DbfTableWriter.LoadSchema(path);
            DbfTableWriter.WriteTable(path, schema, Grupe.ToList(),
                (s, f) => f.ToUpperInvariant() switch
                {
                    "AGPOD"   => (object?)s.AgPod,
                    "AG"      => s.Ag,
                    "OPIS"    => s.Opis,
                    "PRENETO" => s.Preneto,
                    "IDBR"    => (object?)s.IDBr,
                    _         => null
                });
            Poruka = $"Sačuvano {Grupe.Count} zapisa.";
            _log.Information("osagpod.dbf: sačuvano {Count} zapisa", Grupe.Count);
        }
        catch (Exception ex) { Poruka = $"Greška pri snimanju: {ex.Message}"; _log.Error(ex, "Greška pri snimanju osagpod.dbf"); }
    }

    [RelayCommand]
    private void Dodaj()
    {
        var max = Grupe.Count == 0 ? 0 : Grupe.Max(x => x.IDBr);
        var g = new OsAgPodStavka { IDBr = max + 1, Preneto = "N" };
        Grupe.Add(g);
        IzabranaGrupa = g;
        Poruka = "Novi red dodan. Unesite podatke i kliknite Sačuvaj.";
    }

    [RelayCommand]
    private void Obrisi()
    {
        if (IzabranaGrupa == null) { Poruka = "Nije izabran red."; return; }
        if (MessageBox.Show($"Obrisati grupu '{IzabranaGrupa.AgPod} — {IzabranaGrupa.Opis}'?",
                "Brisanje", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Grupe.Remove(IzabranaGrupa);
        IzabranaGrupa = null;
        Poruka = "Red obrisan. Kliknite Sačuvaj da biste snimili izmjene.";
    }

    [RelayCommand]
    private void Osvezi() => Ucitaj();

    [RelayCommand]
    private void Prvi() { if (Grupe.Count > 0) IzabranaGrupa = Grupe[0]; }

    [RelayCommand]
    private void Zadnji() { if (Grupe.Count > 0) IzabranaGrupa = Grupe[^1]; }

    [RelayCommand]
    private void Gore()
    {
        if (IzabranaGrupa == null) return;
        var idx = Grupe.IndexOf(IzabranaGrupa);
        if (idx > 0) IzabranaGrupa = Grupe[idx - 1];
    }

    [RelayCommand]
    private void Dole()
    {
        if (IzabranaGrupa == null) return;
        var idx = Grupe.IndexOf(IzabranaGrupa);
        if (idx >= 0 && idx < Grupe.Count - 1) IzabranaGrupa = Grupe[idx + 1];
    }

    private string? DbfPutanja(string ime) => DbfHelper.NadjiDbf(_appState, ime);
}
