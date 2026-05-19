using CommunityToolkit.Mvvm.ComponentModel;

namespace OsnovnaSredstva.Models;

public class OsKartica : ObservableObject
{
    private string _osifra = "";
    private string _naz = "";
    private DateTime? _datNab;
    private string _brNal = "";
    private string _konto = "";
    private string _vrsta = "";
    private string _ag = "";
    private string _agPod = "";
    private string _invBroj = "";
    private string _mesto = "";
    private decimal _nab0;
    private decimal _isp0;
    private decimal _sad0;
    private decimal _kom;
    private decimal _cena;
    private decimal _stopaOt;
    private string _osnovKor = "";
    private string _izvor = "";

    public string   Osifra   { get => _osifra;   set => SetProperty(ref _osifra,   value); }
    public string   Naz      { get => _naz;       set => SetProperty(ref _naz,      value); }
    public DateTime? DatNab  { get => _datNab;    set => SetProperty(ref _datNab,   value); }
    public string   BrNal    { get => _brNal;     set => SetProperty(ref _brNal,    value); }
    public string   Konto    { get => _konto;     set => SetProperty(ref _konto,    value); }
    public string   Vrsta    { get => _vrsta;     set => SetProperty(ref _vrsta,    value); }
    public string   Ag       { get => _ag;        set => SetProperty(ref _ag,       value); }
    public string   AgPod    { get => _agPod;     set => SetProperty(ref _agPod,    value); }
    public string   InvBroj  { get => _invBroj;   set => SetProperty(ref _invBroj,  value); }
    public string   Mesto    { get => _mesto;     set => SetProperty(ref _mesto,    value); }
    public decimal  Nab0     { get => _nab0;      set => SetProperty(ref _nab0,     value); }
    public decimal  Isp0     { get => _isp0;      set => SetProperty(ref _isp0,     value); }
    public decimal  Sad0     { get => _sad0;      set => SetProperty(ref _sad0,     value); }
    public decimal  Kom      { get => _kom;       set => SetProperty(ref _kom,      value); }
    public decimal  Cena     { get => _cena;      set => SetProperty(ref _cena,     value); }
    public decimal  StopaOt  { get => _stopaOt;   set => SetProperty(ref _stopaOt,  value); }
    public string   OsnovKor { get => _osnovKor;  set => SetProperty(ref _osnovKor, value); }
    public string   Izvor    { get => _izvor;     set => SetProperty(ref _izvor,    value); }

    public string Preneto { get; set; } = "";
    public int    IDBr    { get; set; }

    // Čuva sva ostala polja iz DBF-a za round-trip čuvanje bez gubitka podataka
    public Dictionary<string, object?> ExtraPolja { get; } = new(StringComparer.OrdinalIgnoreCase);
}
