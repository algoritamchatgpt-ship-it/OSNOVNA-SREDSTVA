using OsnovnaSredstva.ViewModels;
using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class FormiranjeLozinkiWindow : Window
{
    public FormiranjeLozinkiWindow(FormiranjeLozinkiViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
