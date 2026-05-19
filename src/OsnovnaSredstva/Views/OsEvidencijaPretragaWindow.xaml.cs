using System.Windows;

namespace OsnovnaSredstva.Views;

public partial class OsEvidencijaPretragaWindow : Window
{
    public string Unos => TxtUnos.Text ?? string.Empty;

    public OsEvidencijaPretragaWindow(string naslov, string prompt, int maxLength)
    {
        InitializeComponent();
        Title = naslov;
        LblPrompt.Text = prompt;
        TxtUnos.MaxLength = maxLength > 0 ? maxLength : int.MaxValue;
        Loaded += (_, _) => TxtUnos.Focus();
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
