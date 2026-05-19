using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class OsPodaciWindow : Window
{
    public OsPodaciWindow(OsPodaciViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnIzlazClick(object sender, RoutedEventArgs e) => Close();
}
