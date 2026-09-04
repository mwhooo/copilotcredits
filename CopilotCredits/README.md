# Copilot Credits

A lightweight cross-platform desktop application that displays your used GitHub Copilot AI credits, automatically refreshing from your GitHub Copilot settings.

## ✅ Platform Support

**Supported on Windows, macOS, and Linux**
- Built with .NET 10.0 and Avalonia (cross-platform GUI framework)
- Runs on any OS with .NET 10.0 runtime installed

## Prerequisites

- **[.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download)** - Required to build and run the app (Windows, macOS, Linux)
- **GitHub account** - With Copilot subscription
- **Chromium browser** (installed automatically via setup below)

## Setup (First Time Only)

1. **Clone/download the repository** and navigate to the project directory

2. **Install Playwright Chromium browser:**
   ```bash
   dotnet run
   ```
   
   The app will automatically download and install Chromium on first run (approximately 300MB).

## Run

```bash
dotnet run --project .\CopilotCredits.csproj
```

Or if you have the app built:
```bash
./bin/Debug/net10.0/CopilotCredits
```

The app will:
- Display your used AI credits in a small window
- Automatically refresh every 1 minute
- Run Chromium **headlessly** (no browser window) if your GitHub session is cached
- Open a browser if GitHub sign-in is required

## Browser Profile

Your GitHub browser session is stored locally at:

**Windows:** `%LOCALAPPDATA%\CopilotCredits\browser-profile`  
**macOS:** `~/.local/share/CopilotCredits/browser-profile`  
**Linux:** `~/.local/share/CopilotCredits/browser-profile`

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Chromium not found" error | The app will automatically install it on first run. Ensure you have ~300MB disk space |
| Browser window opens repeatedly | Sign in to GitHub when prompted. Your session will be cached for future runs |
| Credits won't load | Ensure your GitHub Copilot subscription is active and you're signed into GitHub |
| App won't start on macOS | Ensure .NET 10.0 is installed: `dotnet --version` |

## Development

Built with:
- **.NET 10.0** - Cross-platform runtime
- **[Avalonia](https://avaloniaui.net/)** - Cross-platform desktop UI framework
- **[Playwright](https://playwright.dev/)** - Browser automation for scraping credits