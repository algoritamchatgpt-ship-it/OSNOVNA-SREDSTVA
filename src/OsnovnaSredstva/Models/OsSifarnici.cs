using CommunityToolkit.Mvvm.ComponentModel;

namespace OsnovnaSredstva.Models;

public class OsVrstaStavka : ObservableObject
{
    private string _vrsta = "";
    private string _naziv = "";

    public string Vrsta { get => _vrsta; set => SetProperty(ref _vrsta, value); }
    public string Naziv { get => _naziv; set => SetProperty(ref _naziv, value); }
    public string Preneto { get; set; } = "";
    public int IDBr { get; set; }
}

public class OsAgStavka : ObservableObject
{
    private string _ag = "";
    private decimal _agStopa;
    private string _opis = "";
    private string _vrsta = "";

    public string Ag { get => _ag; set => SetProperty(ref _ag, value); }
    public decimal AgStopa { get => _agStopa; set => SetProperty(ref _agStopa, value); }
    public string Opis { get => _opis; set => SetProperty(ref _opis, value); }
    public string Vrsta { get => _vrsta; set => SetProperty(ref _vrsta, value); }
    public string Preneto { get; set; } = "";
    public int IDBr { get; set; }
}

public class OsAgPodStavka : ObservableObject
{
    private string _agPod = "";
    private string _ag = "";
    private string _opis = "";

    public string AgPod { get => _agPod; set => SetProperty(ref _agPod, value); }
    public string Ag { get => _ag; set => SetProperty(ref _ag, value); }
    public string Opis { get => _opis; set => SetProperty(ref _opis, value); }
    public string Preneto { get; set; } = "";
    public int IDBr { get; set; }
}

public class LdGrupaStavka : ObservableObject
{
    private int _grupa;
    private string _naziv = "";

    public int Grupa { get => _grupa; set => SetProperty(ref _grupa, value); }
    public string Naziv { get => _naziv; set => SetProperty(ref _naziv, value); }
    public string Preneto { get; set; } = "";
    public int IDBr { get; set; }
}

public class OsOsnKStavka : ObservableObject
{
    private string _osnovKor = "";
    private string _naziv = "";

    public string OsnovKor { get => _osnovKor; set => SetProperty(ref _osnovKor, value); }
    public string Naziv { get => _naziv; set => SetProperty(ref _naziv, value); }
    public string Preneto { get; set; } = "";
    public int IDBr { get; set; }
}
