using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class IzvozTabelaWindow : Window
{
    public IzvozTabelaWindow(IzvozTabelaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
