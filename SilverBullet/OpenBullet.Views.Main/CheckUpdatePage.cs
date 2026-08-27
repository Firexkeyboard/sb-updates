using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using OpenBullet.Views.CustomMessageBox;

namespace OpenBullet.Views.Main;

public partial class CheckUpdatePage : Page
{
    private const string ApiUrl = "https://api.github.com/repos/Firexkeyboard/sb-updates/releases/latest";
    private string _downloadUrl = "";

    public CheckUpdatePage()
    {
        InitializeComponent();
        lblCurrentVersion.Text = $"v{SB.Version}";
    }

    // Called externally by MainWindow on startup
    public void CheckForUpdate() => CheckForUpdate_Click(null, null);

    private void CheckForUpdate_Click(object sender, RoutedEventArgs e)
    {
        btnCheck.IsEnabled = false;
        runUpdaterButton.IsEnabled = false;
        lblLatestVersion.Text = "...";
        lblLatestVersion.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
        SetStatus("Checking for updates...", "#888888");
        richTextBox.Document.Blocks.Clear();

        Task.Run(FetchUpdate).ContinueWith(_ =>
            Dispatcher.Invoke(() => btnCheck.IsEnabled = true));
    }

    private async Task FetchUpdate()
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd($"SilverBullet/{SB.Version}");

            var response = await http.GetAsync(ApiUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Dispatcher.Invoke(() =>
                {
                    lblLatestVersion.Text = "—";
                    SetStatus("No releases published yet.", "#888888");
                });
                return;
            }
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            var release = JObject.Parse(json);

            string tagName    = release["tag_name"]?.ToString() ?? "—";
            string remoteStr  = tagName.TrimStart('v');
            string body       = release["body"]?.ToString() ?? "";

            string dlUrl = "";
            if (release["assets"] is JArray assets && assets.Count > 0)
                dlUrl = assets[0]["browser_download_url"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(dlUrl))
                dlUrl = release["html_url"]?.ToString() ?? "";

            bool isNewer = Version.TryParse(remoteStr, out var remote) &&
                           Version.TryParse(SB.Version, out var local) &&
                           remote > local;

            Dispatcher.Invoke(() =>
            {
                _downloadUrl = dlUrl;
                lblLatestVersion.Text = tagName;

                if (isNewer)
                {
                    lblLatestVersion.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xC8, 0x00));
                    SetStatus("✓  A new version is available!", "#00C800");
                    runUpdaterButton.IsEnabled = !string.IsNullOrEmpty(dlUrl);
                }
                else
                {
                    lblLatestVersion.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                    SetStatus("✓  You are running the latest version.", "#00D4FF");
                }

                PopulateReleaseNotes(body);
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => SetStatus($"Error: {ex.Message}", "#FF6B35"));
        }
    }

    private void SetStatus(string text, string hexColor)
    {
        lblStatus.Text = text;
        try { lblStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor)); }
        catch { }
    }

    private void PopulateReleaseNotes(string body)
    {
        richTextBox.Document.Blocks.Clear();
        if (string.IsNullOrWhiteSpace(body)) return;

        foreach (string line in body.Replace("\r\n", "\n").Split('\n'))
        {
            string text = line.Trim();
            if (string.IsNullOrEmpty(text)) continue;

            var para = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
            para.SetResourceReference(TextElement.ForegroundProperty, "ForegroundMain");
            para.Inlines.Add(new Run(text));
            richTextBox.Document.Blocks.Add(para);
        }
    }

    private void RunUpdater_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_downloadUrl)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_downloadUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            CustomMsgBox.ShowError(ex.Message);
        }
    }
}
