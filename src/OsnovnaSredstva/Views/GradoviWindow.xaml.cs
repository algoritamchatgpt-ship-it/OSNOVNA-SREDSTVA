using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class GradoviWindow : Window
{
    public GradoviWindow(GradoviViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
