# GridSurf

Run up to **8 independent WhatsApp Web sessions** side by side in one lightweight desktop window.

No Electron. No bundled Chromium. Just your system's Edge WebView2 runtime — already installed on Windows 10/11.

![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

![GridSurf Screenshot](screenshots/gridsurf-demo.png)

## Why GridSurf?

| Problem | GridSurf |
|---------|----------|
| WhatsApp Desktop only supports 1 account | Up to 8 accounts simultaneously |
| Electron-based alternatives eat 1GB+ RAM | ~300-400MB for 2 sessions |
| Browser tabs share cookies — can't multi-login | Each pane has fully isolated sessions |
| Opening multiple browser profiles is clunky | One window, clean grid layout, done |

## Features

- **8 independent sessions** — each pane has its own cookies, localStorage, IndexedDB. Log into 8 different WhatsApp accounts
- **Lightweight** — uses system Edge WebView2 runtime (already on your PC). No bundled browser engine
- **Memory optimized** — Chromium flags tuned for minimal RAM. Only active panes consume resources
- **Persistent login** — close the app, reopen it, you're still logged in. No re-scanning QR codes
- **Flexible layout** — choose 1 to 8 panes. Grid auto-adjusts (1, 2 side-by-side, 2x2, 3+2, 4+4, etc.)
- **URL bar per pane** — not just WhatsApp. Load any website in any pane (Gmail, Telegram, Discord, etc.)
- **Auto-granted permissions** — notifications, clipboard paste, file downloads all work out of the box
- **Crash recovery** — if a renderer crashes, the pane auto-reloads
- **Zero telemetry** — no tracking, no analytics, no phoning home. Your data stays on your machine

## Download

### Option 1: Download the release (recommended)

Go to [Releases](../../releases) and download the latest `GridSurf.zip`. Extract and run `GridSurf.exe`.

**Requirements:**
- Windows 10/11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (if not already installed)
- Edge WebView2 Runtime (pre-installed on Windows 10/11)

### Option 2: Build from source

**Prerequisites:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (not just runtime — you need the SDK to build)
- Git

```bash
git clone https://github.com/user/GridSurf.git
cd GridSurf
dotnet restore
dotnet build -c Release
```

**Publish as standalone exe:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

Run it:

```bash
bin\Release\net8.0-windows\GridSurf.exe
```

## Usage

1. Launch GridSurf
2. Two WhatsApp Web panes appear side by side
3. Scan QR code in each pane with a different phone
4. Done — two WhatsApp accounts in one window

### Settings (Ctrl + ,)

- Change URLs for any pane (WhatsApp, Gmail, Telegram, Discord, etc.)
- Set visible pane count (1–8)
- Unused panes are suspended — zero RAM wasted

## How it works

Each pane creates an isolated [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) environment with a separate user data folder:

```
~/.gridsurf/
├── session1/    # Pane 1 cookies, storage, cache
├── session2/    # Pane 2 cookies, storage, cache
├── ...
└── session8/    # Pane 8 cookies, storage, cache
```

Sessions are completely independent. Different cookies, different logins, different everything.

## Memory usage

| Panes | Typical RAM |
|-------|------------|
| 1 | ~200MB |
| 2 | ~350-450MB |
| 4 | ~600-800MB |
| 8 | ~1.2-1.6GB |

For comparison, 2 Electron-based WhatsApp windows typically use 1-1.5GB.

## Tech stack

- **C# / .NET 8** — WPF desktop app
- **Microsoft WebView2** — Edge-based web rendering (uses system runtime, not bundled)
- **Zero dependencies** beyond WebView2 NuGet package

## Project structure

```
GridSurf/
├── App.xaml / App.xaml.cs           # Application entry
├── MainWindow.xaml / .xaml.cs       # Main window, pane grid, WebView2 lifecycle
├── SettingsStore.cs                 # Load/save settings, session migration
├── SettingsWindow.xaml / .xaml.cs   # Settings dialog
├── GridSurf.csproj                 # Project file
├── build.bat / build.ps1           # Build scripts
└── .gitignore
```

## Contributing

Found a bug? Want a feature? Open an issue or PR.

This was built to scratch my own itch — running multiple WhatsApp accounts without bloated apps eating my RAM. If it helps you too, that's a win.

## License

[MIT](LICENSE) — do whatever you want with it.
