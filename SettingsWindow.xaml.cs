using System.Windows;
using System.Windows.Controls;

namespace GridSurf;

public partial class SettingsWindow : Window
{
    private const int MaxPanes = 8;
    private readonly TextBox[] _urlBoxes = new TextBox[MaxPanes];

    public string[] UrlsResult { get; private set; } = [];
    public int ViewCountResult { get; private set; } = 2;

    public SettingsWindow(IReadOnlyList<string> urls, int viewCount)
    {
        InitializeComponent();
        ViewCountBox.Text = viewCount.ToString();

        for (var i = 0; i < MaxPanes; i++)
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            var label = new TextBlock
            {
                Text = $"Pane {i + 1} URL",
                Width = 90,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var tb = new TextBox
            {
                Text = i < urls.Count ? urls[i] : "",
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(6, 4, 6, 4),
            };
            _urlBoxes[i] = tb;

            DockPanel.SetDock(label, Dock.Left);
            row.Children.Add(label);
            row.Children.Add(tb);
            UrlPanel.Children.Add(row);
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        UrlsResult = new string[MaxPanes];
        for (var i = 0; i < MaxPanes; i++)
            UrlsResult[i] = _urlBoxes[i].Text;
        ViewCountResult = int.TryParse(ViewCountBox.Text, out var vc) ? Math.Clamp(vc, 1, MaxPanes) : 2;
        DialogResult = true;
    }
}
