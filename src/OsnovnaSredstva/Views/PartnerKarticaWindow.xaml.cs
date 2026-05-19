using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class PartnerKarticaWindow : Window
{
    public PartnerKarticaWindow(PartnerKarticaViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
