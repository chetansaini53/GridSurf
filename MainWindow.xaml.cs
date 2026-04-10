using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace GridSurf;

public partial class MainWindow : Window
{
    private const int MaxPanes = 8;

    private const string ChromeUA =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

    private static readonly string BrowserArgs = string.Join(" ", [
        "--disable-gpu-compositing",
        "--disable-software-rasterizer",
        "--js-flags=--max-old-space-size=128",
        "--disable-features=TranslateUI",
        "--disable-background-networking",
        "--disable-breakpad",
        "--disable-component-update",
        "--disable-default-apps",
        "--disable-extensions",
        "--disable-sync",
        "--autoplay-policy=no-user-gesture-required",
        "--process-per-site",
    ]);

    private readonly string[] _sessionFolders = new string[MaxPanes];
    private readonly Grid[] _paneContainers = new Grid[MaxPanes];
    private readonly WebView2[] _views = new WebView2[MaxPanes];
    private readonly TextBox[] _urlBars = new TextBox[MaxPanes];
    private readonly bool[] _viewInitialized = new bool[MaxPanes];
    private bool _loaded;

    public MainWindow()
    {
        InitializeComponent();
        SettingsStore.MigrateLegacyDir();

        for (var i = 0; i < MaxPanes; i++)
        {
            _sessionFolders[i] = Path.Combine(SettingsStore.BaseDir, $"session{i + 1}");
            BuildPaneUI(i);
        }

        Loaded += MainWindow_Loaded;
        KeyDown += MainWindow_KeyDown;
    }

    private void BuildPaneUI(int index)
    {
        var urlBar = new TextBox
        {
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 4, 6, 4),
            Tag = index.ToString(),
        };
        urlBar.KeyDown += UrlBar_KeyDown;
        _urlBars[index] = urlBar;

        var goBtn = new Button { Content = "Go", Width = 44, Margin = new Thickness(4, 0, 0, 0), Tag = index.ToString() };
        goBtn.Click += UrlGo_Click;

        var dock = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 0, 0, 2) };
        DockPanel.SetDock(goBtn, Dock.Right);
        dock.Children.Add(goBtn);
        dock.Children.Add(urlBar);

        var webView = new WebView2();
        _views[index] = webView;

        var container = new Grid { Margin = new Thickness(1) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(dock, 0);
        Grid.SetRow(webView, 1);
        container.Children.Add(dock);
        container.Children.Add(webView);

        _paneContainers[index] = container;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control)
            OpenSettings();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var settings = SettingsStore.Load();
        for (var i = 0; i < MaxPanes; i++)
            _urlBars[i].Text = settings.Urls[i];

        ApplyLayout(settings.ViewCount);
        await InitActiveViewsAsync(settings);
        _loaded = true;
    }

    private async Task InitActiveViewsAsync(SettingsStore.Settings settings)
    {
        var opts = new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = BrowserArgs };

        for (var i = 0; i < MaxPanes; i++)
        {
            if (_paneContainers[i].Visibility != Visibility.Visible) continue;
            if (_viewInitialized[i]) continue;

            Directory.CreateDirectory(_sessionFolders[i]);
            var env = await CoreWebView2Environment.CreateAsync(null, _sessionFolders[i], opts);
            await _views[i].EnsureCoreWebView2Async(env);

            var wv2 = _views[i].CoreWebView2!;
            wv2.Settings.UserAgent = ChromeUA;
            wv2.Settings.IsStatusBarEnabled = false;
            wv2.Settings.AreDefaultContextMenusEnabled = true;
            wv2.Settings.IsBuiltInErrorPageEnabled = true;

            var idx = i;
            wv2.PermissionRequested += (_, args) =>
            {
                if (args.PermissionKind is CoreWebView2PermissionKind.Notifications
                    or CoreWebView2PermissionKind.ClipboardRead)
                {
                    args.State = CoreWebView2PermissionState.Allow;
                }
            };

            wv2.DownloadStarting += (_, args) => { args.Handled = false; };

            wv2.NavigationCompleted += (_, _) =>
            {
                try
                {
                    var src = _views[idx].CoreWebView2?.Source;
                    if (src != null) _urlBars[idx].Text = src;
                }
                catch { /* ignore */ }
            };

            wv2.ProcessFailed += (_, args) =>
            {
                if (args.ProcessFailedKind is CoreWebView2ProcessFailedKind.RenderProcessExited
                    or CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
                {
                    try { _views[idx].Reload(); } catch { /* ignore */ }
                }
            };

            _viewInitialized[i] = true;
            wv2.Navigate(SettingsStore.NormalizeUrl(settings.Urls[i]));
        }
    }

    private void ApplyLayout(int paneCount)
    {
        paneCount = Math.Clamp(paneCount, 1, MaxPanes);

        PaneGrid.Children.Clear();
        PaneGrid.RowDefinitions.Clear();
        PaneGrid.ColumnDefinitions.Clear();

        for (var i = 0; i < MaxPanes; i++)
            _paneContainers[i].Visibility = i < paneCount ? Visibility.Visible : Visibility.Collapsed;

        if (paneCount == 1)
        {
            PaneGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            PaneGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_paneContainers[0], 0);
            Grid.SetColumn(_paneContainers[0], 0);
            PaneGrid.Children.Add(_paneContainers[0]);
            return;
        }

        if (paneCount == 2)
        {
            PaneGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            PaneGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            PaneGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_paneContainers[0], 0); Grid.SetColumn(_paneContainers[0], 0);
            Grid.SetRow(_paneContainers[1], 0); Grid.SetColumn(_paneContainers[1], 1);
            PaneGrid.Children.Add(_paneContainers[0]);
            PaneGrid.Children.Add(_paneContainers[1]);
            return;
        }

        var topCount = (paneCount + 1) / 2;
        var bottomCount = paneCount - topCount;
        var cols = topCount;

        PaneGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        PaneGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        for (var c = 0; c < cols; c++)
            PaneGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < topCount; i++)
        {
            Grid.SetRow(_paneContainers[i], 0);
            Grid.SetColumn(_paneContainers[i], i);
            Grid.SetColumnSpan(_paneContainers[i], 1);
            PaneGrid.Children.Add(_paneContainers[i]);
        }

        if (bottomCount == topCount)
        {
            for (var i = 0; i < bottomCount; i++)
            {
                Grid.SetRow(_paneContainers[topCount + i], 1);
                Grid.SetColumn(_paneContainers[topCount + i], i);
                Grid.SetColumnSpan(_paneContainers[topCount + i], 1);
                PaneGrid.Children.Add(_paneContainers[topCount + i]);
            }
        }
        else
        {
            var remaining = cols;
            for (var i = 0; i < bottomCount; i++)
            {
                var panesLeft = bottomCount - i;
                var span = remaining / panesLeft;
                var col = cols - remaining;
                Grid.SetRow(_paneContainers[topCount + i], 1);
                Grid.SetColumn(_paneContainers[topCount + i], col);
                Grid.SetColumnSpan(_paneContainers[topCount + i], span);
                PaneGrid.Children.Add(_paneContainers[topCount + i]);
                remaining -= span;
            }
        }
    }

    private void UrlBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox tb || tb.Tag is not string tag || !int.TryParse(tag, out var idx)) return;
        e.Handled = true;
        NavigateFromBar(idx);
    }

    private void UrlGo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not string tag || !int.TryParse(tag, out var idx)) return;
        NavigateFromBar(idx);
    }

    private void NavigateFromBar(int index)
    {
        if (!_viewInitialized[index]) return;
        var url = SettingsStore.NormalizeUrl(_urlBars[index].Text);
        _urlBars[index].Text = url;
        _views[index].CoreWebView2?.Navigate(url);
        SaveCurrentUrls();
    }

    private void SaveCurrentUrls()
    {
        var settings = SettingsStore.Load();
        for (var i = 0; i < MaxPanes; i++)
            settings.Urls[i] = _urlBars[i].Text;
        SettingsStore.Save(settings);
    }

    private void MenuSettings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private async void OpenSettings()
    {
        if (!_loaded) return;
        var settings = SettingsStore.Load();
        var dlg = new SettingsWindow(settings.Urls, settings.ViewCount) { Owner = this };
        if (dlg.ShowDialog() != true) return;

        var newSettings = new SettingsStore.Settings { Urls = dlg.UrlsResult, ViewCount = dlg.ViewCountResult };
        SettingsStore.Save(newSettings);

        for (var i = 0; i < MaxPanes; i++)
            _urlBars[i].Text = newSettings.Urls[i];

        ApplyLayout(newSettings.ViewCount);
        await InitActiveViewsAsync(newSettings);

        for (var i = 0; i < MaxPanes; i++)
        {
            if (_paneContainers[i].Visibility == Visibility.Visible && _viewInitialized[i])
                _views[i].CoreWebView2?.Navigate(SettingsStore.NormalizeUrl(newSettings.Urls[i]));
        }
    }
}
