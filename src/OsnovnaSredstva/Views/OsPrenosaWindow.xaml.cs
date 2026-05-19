using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class OsPrenosaWindow : Window
{
    public OsPrenosaWindow(OsPrenosaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.Log.CollectionChanged += (_, _) =>
        {
            if (LogBox.Items.Count > 0)
                LogBox.ScrollIntoView(LogBox.Items[LogBox.Items.Count - 1]);
        };
    }

    private void OnZatvoriClick(object sender, RoutedEventArgs e) => Close();
}
