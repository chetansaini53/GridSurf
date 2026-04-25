using System.IO;
using System.Text.Json;

namespace GridSurf;

internal static class SettingsStore
{
    private const int MaxPanes = 8;
    private const string Whatsapp = "https://web.whatsapp.com";

    private static readonly string[] LegacyDirs =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".octochat"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".lodabrowser2"),
    ];

    internal static string BaseDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gridsurf");

    internal static string SharedUdfDir =>
        Path.Combine(BaseDir, "webview2");

    private static string SettingsPath => Path.Combine(BaseDir, "settings.json");

    internal static string ProfileNameFor(int paneIndex) => $"WA{paneIndex + 1}";

    internal static bool LegacyPerPaneSessionsExist()
    {
        if (!Directory.Exists(BaseDir)) return false;
        for (var i = 1; i <= MaxPanes; i++)
        {
            if (Directory.Exists(Path.Combine(BaseDir, $"session{i}"))) return true;
        }
        return false;
    }

    internal static void ArchiveLegacyPerPaneSessions()
    {
        if (!Directory.Exists(BaseDir)) return;
        var archiveRoot = Path.Combine(BaseDir, "legacy-sessions-backup");
        Directory.CreateDirectory(archiveRoot);
        for (var i = 1; i <= MaxPanes; i++)
        {
            var src = Path.Combine(BaseDir, $"session{i}");
            if (!Directory.Exists(src)) continue;
            var dest = Path.Combine(archiveRoot, $"session{i}");
            try
            {
                if (Directory.Exists(dest)) Directory.Delete(dest, true);
                Directory.Move(src, dest);
            }
            catch { /* skip locked dirs; user can clean manually */ }
        }
    }

    internal sealed class Settings
    {
        public string[] Urls { get; set; } = DefaultUrls();
        public int ViewCount { get; set; } = 2;
    }

    internal static void MigrateLegacyDir()
    {
        if (Directory.Exists(BaseDir)) return;
        foreach (var legacy in LegacyDirs)
        {
            if (!Directory.Exists(legacy)) continue;
            try { Directory.Move(legacy, BaseDir); return; }
            catch { /* if move fails, try next */ }
        }
    }

    internal static string[] DefaultUrls()
    {
        return
        [
            Whatsapp,
            Whatsapp,
            "https://mail.google.com",
            "https://calendar.google.com",
            Whatsapp,
            Whatsapp,
            "https://web.telegram.org",
            "https://discord.com/app",
        ];
    }

    internal static string NormalizeUrl(string? s)
    {
        s = (s ?? "").Trim();
        if (string.IsNullOrEmpty(s)) return Whatsapp;
        if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "https://" + s;
        return s;
    }

    internal static Settings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new Settings();
            var json = File.ReadAllText(SettingsPath);
            var doc = JsonSerializer.Deserialize<SettingsFile>(json);
            var urls = new string[MaxPanes];
            var def = DefaultUrls();
            for (var i = 0; i < MaxPanes; i++)
                urls[i] = doc?.Urls is { Length: > 0 } && i < doc.Urls.Length
                    ? NormalizeUrl(doc.Urls[i])
                    : def[i];
            var vc = Math.Clamp(doc?.ViewCount ?? 2, 1, MaxPanes);
            return new Settings { Urls = urls, ViewCount = vc };
        }
        catch
        {
            return new Settings();
        }
    }

    internal static void Save(Settings settings)
    {
        Directory.CreateDirectory(BaseDir);
        var def = DefaultUrls();
        var u = new string[MaxPanes];
        for (var i = 0; i < MaxPanes; i++)
            u[i] = i < settings.Urls.Length ? NormalizeUrl(settings.Urls[i]) : def[i];
        var payload = new SettingsFile { Urls = u, ViewCount = Math.Clamp(settings.ViewCount, 1, MaxPanes) };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    private sealed class SettingsFile
    {
        public string[] Urls { get; set; } = [];
        public int ViewCount { get; set; } = 2;
    }
}
