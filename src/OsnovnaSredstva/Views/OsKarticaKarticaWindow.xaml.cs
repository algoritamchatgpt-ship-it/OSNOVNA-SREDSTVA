using OsnovnaSredstva.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OsnovnaSredstva.Views;

public partial class OsKarticaKarticaWindow : Window
{
    public OsKarticaKarticaWindow(OsKarticaKarticaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F1 && e.Key != Key.F4)
            return;

        if (DataContext is not OsKarticaKarticaViewModel vm)
            return;

        if (Keyboard.FocusedElement is not TextBox tb)
            return;

        var command = tb.Name switch
        {
            nameof(VrstaBox) => vm.LookupVrstaCommand,
            nameof(OsnovKorBox) => vm.LookupOsnovKoriscenjaCommand,
            nameof(IzvorBox) => vm.LookupIzvorFinansiranjaCommand,
            nameof(MestoLokacijeBox) => vm.LookupMestoLokacijeCommand,
            nameof(MestoTroskovaBox) => vm.LookupMestoCommand,
            nameof(KontoBox) => vm.LookupKontoCommand,
            nameof(AgBox) => vm.LookupAmortizacionaGrupaCommand,
            nameof(AgPodBox) => vm.LookupAmortizacionaPodgrupaCommand,
            _ => null
        };

        if (command == null || !command.CanExecute(this))
            return;

        command.Execute(this);
        e.Handled = true;
    }
}
