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

public class OsIzvorStavka : ObservableObject
{
    private string _izvor = "";
    private string _naziv = "";

    public string Izvor { get => _izvor; set => SetProperty(ref _izvor, value); }
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

public class OsOaStavka : ObservableObject
{
    private string _ag = "";
    private decimal _pocetno;
    private decimal _nabavka;
    private decimal _prodaja;
    private decimal _neotpis;
    private decimal _agStopa;
    private decimal _amort2;
    private decimal _sad2;
    private string _preneto = "";
    private int _numred;
    private int _idbr;

    public string  Ag      { get => _ag;      set => SetProperty(ref _ag,      value); }
    public decimal Pocetno { get => _pocetno;  set => SetProperty(ref _pocetno,  value); }
    public decimal Nabavka { get => _nabavka;  set => SetProperty(ref _nabavka,  value); }
    public decimal Prodaja { get => _prodaja;  set => SetProperty(ref _prodaja,  value); }
    public decimal Neotpis { get => _neotpis;  set => SetProperty(ref _neotpis,  value); }
    public decimal AgStopa { get => _agStopa;  set => SetProperty(ref _agStopa,  value); }
    public decimal Amort2  { get => _amort2;   set => SetProperty(ref _amort2,   value); }
    public decimal Sad2    { get => _sad2;     set => SetProperty(ref _sad2,     value); }
    public string  Preneto { get => _preneto;  set => SetProperty(ref _preneto,  value); }
    public int     Numred  { get => _numred;   set => SetProperty(ref _numred,   value); }
    public int     IDBr    { get => _idbr;     set => SetProperty(ref _idbr,     value); }
}

public class OsSaldoStavka
{
    public string  Sifra       { get; set; } = "";
    public int     BrojKartica { get; set; }
    public decimal Nab0  { get; set; }
    public decimal Isp0  { get; set; }
    public decimal Sad0  { get; set; }
    public decimal Nab   { get; set; }
    public decimal Isp   { get; set; }
    public decimal Sad   { get; set; }
    public decimal Amort { get; set; }
    public decimal Nab02  { get; set; }
    public decimal Isp02  { get; set; }
    public decimal Sad02  { get; set; }
    public decimal Nab2   { get; set; }
    public decimal Isp2   { get; set; }
    public decimal Amort2 { get; set; }
    public decimal Sad2   { get; set; }
}

public class OsMrsRedak
{
    public string  Sifra     { get; set; } = "";
    public string  Naziv     { get; set; } = "";
    public string  Konto     { get; set; } = "";
    public string  Mesto     { get; set; } = "";
    public string  Ag        { get; set; } = "";
    public decimal Nab0      { get; set; }
    public decimal Isp0      { get; set; }
    public decimal Sad0      { get; set; }
    public decimal StopaOt   { get; set; }
    public decimal Amort     { get; set; }
    public decimal Isp       { get; set; }
    public decimal Sad       { get; set; }
    public decimal Nab02     { get; set; }
    public decimal Isp02     { get; set; }
    public decimal Sad02     { get; set; }
    public decimal StopaOt2  { get; set; }
    public decimal Amort2    { get; set; }
    public decimal Isp2      { get; set; }
    public decimal Sad2      { get; set; }
}
