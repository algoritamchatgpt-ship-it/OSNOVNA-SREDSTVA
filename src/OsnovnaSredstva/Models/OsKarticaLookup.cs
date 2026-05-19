using CommunityToolkit.Mvvm.ComponentModel;

namespace OsnovnaSredstva.Models;

public enum OsKarticaLookupTip
{
    Vrsta,
    OsnovKoriscenja,
    IzvorFinansiranja,
    AmortizacionaGrupa,
    AmortizacionaPodgrupa,
    Mesto,
    Konto
}

public partial class OsKarticaLookupStavka : ObservableObject
{
    [ObservableProperty] private string _sifra = string.Empty;
    [ObservableProperty] private string _naziv = string.Empty;
    [ObservableProperty] private string _dodatno = string.Empty;

    public Dictionary<string, object?> OriginalnaPolja { get; } = new(StringComparer.OrdinalIgnoreCase);
}
