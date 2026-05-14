using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OsnovnaSredstva.Controls;

public static class ButtonShortcutService
{
    private static readonly Dictionary<Key, string> ShortcutMap = new()
    {
        { Key.F1, "F1 — Pomoć / Prečice" },
        { Key.F5, "F5 — Osveži" },
        { Key.Escape, "Esc — Zatvori prozor" },
    };

    public static void Initialize() { }

    public static string BuildShortcutOverview(Window window)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Dostupne prečice:\n");
        sb.AppendLine("  Esc / Ctrl+W  — Zatvori prozor");
        sb.AppendLine("  F1            — Ova poruka (pomoć)");
        sb.AppendLine("  Enter         — Potvrdi (glavni dugme)");
        sb.AppendLine("  Backspace     — Prethodno polje (kada je kursor na početku)");
        sb.AppendLine("  Tab / Shift+Tab — Sledeće / prethodno polje");

        var buttons = new List<Button>();
        CollectButtons(window, buttons);

        var eligible = buttons.Where(b => b.IsVisible && b.IsEnabled).ToList();
        if (eligible.Count > 0)
        {
            sb.AppendLine("\nDugmad u prozoru:");
            foreach (var btn in eligible)
            {
                var label = ExtractLabel(btn);
                if (!string.IsNullOrWhiteSpace(label))
                    sb.AppendLine($"  • {label}");
            }
        }

        return sb.ToString();
    }

    private static void CollectButtons(DependencyObject root, List<Button> buttons)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is Button b && b.GetType() == typeof(Button))
                buttons.Add(b);
            if (child is DependencyObject dep)
                CollectButtons(dep, buttons);
        }
    }

    private static string ExtractLabel(Button button) =>
        button.Content switch
        {
            null => string.Empty,
            string text => text,
            TextBlock tb => tb.Text ?? string.Empty,
            AccessText at => at.Text ?? string.Empty,
            _ => button.Content.ToString() ?? string.Empty
        };
}
