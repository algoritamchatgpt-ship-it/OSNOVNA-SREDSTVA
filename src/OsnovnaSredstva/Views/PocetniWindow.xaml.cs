using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class PocetniWindow : Window
{
    public event Action? UlazKliknut;

    public PocetniWindow(PocetniViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        BtnUlaz.Click += (_, _) => UlazKliknut?.Invoke();
        BtnZatvori.Click += (_, _) => Application.Current.Shutdown();

        // Drag za windowless prozor
        MouseLeftButtonDown += (_, _) => DragMove();
    }
}
