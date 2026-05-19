using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class PartneriWindow : Window
{
    public PartneriWindow(PartneriViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
