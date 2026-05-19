using Microsoft.Extensions.DependencyInjection;
using OsnovnaSredstva.Controls;
using OsnovnaSredstva.Services;
using OsnovnaSredstva.ViewModels;
using Serilog;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace OsnovnaSredstva;

public partial class App : Application
{
    private static readonly string[] ConfirmTokens =
        { "POTVRDI", "SACUVAJ", "SNIMI", "ULAZ", "PRIJAVA", "OK", "DA" };

    private ServiceProvider _serviceProvider = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ButtonShortcutService.Initialize();

        // Backspace na poziciji 0 → Shift+Tab
        EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnTextBoxBackspace));

        // ESC zatvara OS prozore
        EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnWindowEsc));

        // Ctrl+W zatvara OS prozore
        EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnWindowCtrlW));

        // F1 prikazuje prečice
        EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnWindowF1));

        // Enter aktivira confirm dugme
        EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewKeyDownEvent,
            new KeyEventHandler(OnWindowEnter));

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                "logs/os-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("=== FIN OSNOVNA SREDSTVA — pokrenuta ===");

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "CRASH — neobrađena greška");
            MessageBox.Show(
                $"Desila se neočekivana greška.\n\nOpis: {args.Exception.Message}\n\n" +
                "Greška je zabeležena u log fajlu.",
                "Osnovna sredstva — Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        var services = new ServiceCollection();
        KonfigurisServise(services);
        _serviceProvider = services.BuildServiceProvider();

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        PrikaziPocetni();
    }

    private static void KonfigurisServise(ServiceCollection services)
    {
        services.AddSingleton<AppState>();
        services.AddSingleton<IPutanjaService, PutanjaService>();
        services.AddSingleton<IAuthService>(sp =>
            new OsAuthService(sp.GetRequiredService<IPutanjaService>()));
        services.AddSingleton<IFirmaService>(sp =>
            new OsFirmaService(sp.GetRequiredService<IPutanjaService>()));
        services.AddSingleton<ILoginSessionService>(sp =>
            new FileLoginSessionService(sp.GetRequiredService<IPutanjaService>()));

        services.AddTransient<PocetniViewModel>();
        services.AddTransient<LoginViewModel>(sp => new LoginViewModel(
            sp.GetRequiredService<IAuthService>(),
            sp.GetRequiredService<ILoginSessionService>(),
            sp.GetRequiredService<IPutanjaService>(),
            sp.GetRequiredService<AppState>()));
        services.AddTransient<FirmaIzborViewModel>(sp => new FirmaIzborViewModel(
            sp.GetRequiredService<IFirmaService>(),
            sp.GetRequiredService<IPutanjaService>(),
            sp.GetRequiredService<AppState>()));
        services.AddTransient<OsMenuViewModel>(sp =>
            new OsMenuViewModel(sp.GetRequiredService<AppState>(), sp.GetRequiredService<IPutanjaService>()));
    }

    private void PrikaziPocetni()
    {
        Log.Information("Otvaram početni ekran");
        var vm = _serviceProvider.GetRequiredService<PocetniViewModel>();
        var win = new Views.PocetniWindow(vm);
        win.UlazKliknut += () => PrikaziLogin(win);
        win.Show();
    }

    private void PrikaziLogin(Views.PocetniWindow pocetniWin)
    {
        Log.Information("Otvaram login");
        var vm = _serviceProvider.GetRequiredService<LoginViewModel>();
        var win = new Views.LoginWindow(vm);
        win.PrijavaUspela += () =>
        {
            pocetniWin.Close();
            win.Close();
            PrikaziFirmaIzbor();
        };
        win.Show();
    }

    private void PrikaziFirmaIzbor()
    {
        Log.Information("Otvaram izbor firme");
        var vm = _serviceProvider.GetRequiredService<FirmaIzborViewModel>();
        var win = new Views.FirmaIzborWindow(vm);
        win.FirmaIzabrana += () =>
        {
            win.Close();
            PrikaziGlavniMeni();
        };
        win.OtkacenoPrijavljeni += () =>
        {
            win.Close();
            PrikaziPocetni();
        };
        win.Show();
    }

    private void PrikaziGlavniMeni()
    {
        Log.Information("Otvaram glavni meni OS");
        var vm = _serviceProvider.GetRequiredService<OsMenuViewModel>();
        var win = new Views.OsMenuWindow(vm);
        vm.OdjavaSeTrazena += () =>
        {
            win.Close();
            PrikaziPocetni();
        };
        vm.VratiseFirmaIzboru += () =>
        {
            win.Close();
            PrikaziFirmaIzbor();
        };
        win.Show();
    }

    // ─── Globalni handler: Backspace → prethodno polje ───
    private static void OnTextBoxBackspace(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Back) return;
        if (sender is not TextBox tb) return;
        if (tb.SelectionStart != 0 || tb.SelectionLength != 0) return;
        tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
        e.Handled = true;
    }

    // ─── Globalni handler: ESC zatvara OS prozore ───
    private static void OnWindowEsc(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || Keyboard.Modifiers != ModifierKeys.None) return;
        if (sender is Window w && JeOsProzon(w)) { w.Close(); e.Handled = true; }
    }

    // ─── Globalni handler: Ctrl+W zatvara OS prozore ───
    private static void OnWindowCtrlW(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.W || Keyboard.Modifiers != ModifierKeys.Control) return;
        if (sender is Window w && JeOsProzon(w)) { w.Close(); e.Handled = true; }
    }

    // ─── Globalni handler: F1 prikazuje prečice ───
    private static void OnWindowF1(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F1 || Keyboard.Modifiers != ModifierKeys.None) return;
        if (sender is not Window w || !JeOsProzon(w)) return;
        var pregled = ButtonShortcutService.BuildShortcutOverview(w);
        MessageBox.Show(w, pregled, "Brze precice", MessageBoxButton.OK, MessageBoxImage.Information);
        e.Handled = true;
    }

    // ─── Globalni handler: Enter aktivira confirm dugme ───
    private static void OnWindowEnter(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None) return;
        if (sender is not Window w || !JeOsProzon(w)) return;
        if (JeEditKontrol(Keyboard.FocusedElement as DependencyObject)) return;

        var buttons = DajDugmad(w);
        if (buttons.Count == 0) return;

        foreach (var btn in buttons)
        {
            if (!btn.IsDefault) continue;
            btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, btn));
            e.Handled = true;
            return;
        }

        var confirms = buttons.Where(JeConfirmDugme).ToList();
        if (confirms.Count != 1) return;
        confirms[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent, confirms[0]));
        e.Handled = true;
    }

    private static bool JeOsProzon(Window w)
        => w.GetType().Namespace?.Contains(".Views", StringComparison.Ordinal) == true &&
           w.GetType().Namespace?.Contains("OsnovnaSredstva", StringComparison.Ordinal) == true &&
           w.GetType().Name is not ("PocetniWindow" or "LoginWindow");

    private static List<Button> DajDugmad(DependencyObject root)
    {
        var all = new List<Button>();
        PokupiDugmad(root, all);
        return all.Where(b => b.IsVisible && b.IsEnabled).ToList();
    }

    private static void PokupiDugmad(DependencyObject root, List<Button> list)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is Button b && b.GetType() == typeof(Button)) list.Add(b);
            if (child is DependencyObject dep) PokupiDugmad(dep, list);
        }
    }

    private static bool JeEditKontrol(DependencyObject? element)
    {
        var cur = element;
        while (cur != null)
        {
            if (cur is TextBoxBase or PasswordBox or ComboBox or DatePicker or DataGrid or DataGridCell)
                return true;
            cur = cur is FrameworkElement fe && fe.Parent is DependencyObject p ? p
                : cur is FrameworkContentElement fce && fce.Parent is DependencyObject cp ? cp
                : System.Windows.Media.VisualTreeHelper.GetParent(cur);
        }
        return false;
    }

    private static bool JeConfirmDugme(Button btn)
    {
        var label = NormalizujLabel(EkstraktujLabel(btn));
        return ConfirmTokens.Any(t => label.Contains(t, StringComparison.Ordinal));
    }

    private static string EkstraktujLabel(Button btn) =>
        btn.Content switch
        {
            null => string.Empty,
            string s => s,
            TextBlock tb => tb.Text ?? string.Empty,
            AccessText at => at.Text ?? string.Empty,
            _ => btn.Content.ToString() ?? string.Empty
        };

    private static string NormalizujLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var norm = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(norm.Length);
        foreach (var c in norm.ToUpperInvariant())
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.GetService<AppState>()?.Odjavi();
        Log.Information("=== FIN OSNOVNA SREDSTVA — zatvorena ===");
        Log.CloseAndFlush();
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}
