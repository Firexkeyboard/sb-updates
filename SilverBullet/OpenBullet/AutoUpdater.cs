using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace OpenBullet;

public static class AutoUpdater
{
    private const string ApiUrl = "https://api.github.com/repos/Firexkeyboard/sb-updates/releases/latest";

    // Fired on the UI thread when a newer release is found.
    public static event Action<string> UpdateAvailable;

    public static string PendingTagName    { get; private set; }
    public static string PendingDownloadUrl { get; private set; }

    public static void CheckAndPrompt()
    {
        Task.Run(async () =>
        {
            try
            {
                var (available, tagName, downloadUrl) = await CheckAsync();
                if (!available) return;

                PendingTagName     = tagName;
                PendingDownloadUrl = downloadUrl;

                Application.Current.Dispatcher.Invoke(() =>
                    UpdateAvailable?.Invoke(tagName));
            }
            catch { /* no internet or empty repo — fail silently */ }
        });
    }

    public static Task ApplyUpdateAsync() => DownloadAndApply(PendingDownloadUrl);

    private static async Task<(bool available, string tagName, string downloadUrl)> CheckAsync()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("SilverBullet-Updater/1.0");

        string json = await http.GetStringAsync(ApiUrl);
        var release = JObject.Parse(json);

        string tagName = release["tag_name"]?.ToString() ?? "";
        string remoteVersion = tagName.TrimStart('v');

        bool newer = Version.TryParse(remoteVersion, out var remote) &&
                     Version.TryParse(SB.Version, out var local) &&
                     remote > local;

        string downloadUrl = "";
        if (newer && release["assets"] is JArray assets && assets.Count > 0)
            downloadUrl = assets[0]["browser_download_url"]?.ToString() ?? "";

        return (newer && !string.IsNullOrEmpty(downloadUrl), tagName, downloadUrl);
    }

    private static async Task DownloadAndApply(string downloadUrl)
    {
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "sb-update");
            string zipPath = Path.Combine(Path.GetTempPath(), "sb-update.zip");
            string appDir  = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;

            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("SilverBullet-Updater/1.0");

            byte[] data = await http.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(zipPath, data);

            ZipFile.ExtractToDirectory(zipPath, tempDir, overwriteFiles: true);
            File.Delete(zipPath);

            // PowerShell script that runs after SilverBullet closes:
            // waits 2 s, copies new files over the old ones, then relaunches.
            string ps1Path = Path.Combine(Path.GetTempPath(), "sb_apply_update.ps1");
            string script  = $@"
Start-Sleep -Seconds 2
Copy-Item -Path '{tempDir}\*' -Destination '{appDir}\' -Recurse -Force
Start-Process '{exePath}'
Remove-Item -LiteralPath $MyInvocation.MyCommand.Path -Force
";
            await File.WriteAllTextAsync(ps1Path, script);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = "powershell.exe",
                Arguments       = $"-NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{ps1Path}\"",
                UseShellExecute = true,
                WindowStyle     = System.Diagnostics.ProcessWindowStyle.Hidden
            });

            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show($"Update failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
        }
    }
}
