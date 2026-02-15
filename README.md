# Winharpoon

A Harpoon-style window switcher for Windows. Mark windows to slots and switch instantly with global hotkeys.

Inspired by [ThePrimeagen's Harpoon](https://github.com/ThePrimeagen/harpoon) for Neovim.

## Installation

### Prerequisites
- Windows 10 or later
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (if not using self-contained build)

### Download
Download the latest release from the [Releases](https://github.com/andres-m-rodriguez/Winharpoon/releases) page.

### Build from Source
```bash
git clone https://github.com/andres-m-rodriguez/Winharpoon.git
cd Winharpoon
dotnet build
dotnet run --project App/Winharpoon.App.csproj
```

## Usage

### Hotkeys

| Hotkey | Action |
|--------|--------|
| `Ctrl+Shift+M` then `1-9` | Mark current window to slot |
| `Ctrl+Shift+M` then `0` | Clear all marks |
| `Ctrl+Shift+1-9` | Switch to marked window |
| `Ctrl+Shift+N` | Cycle to next marked window |
| `Ctrl+Shift+P` | Cycle to previous marked window |
| `Ctrl+Shift+H` | Show marks overlay |

### System Tray
- Right-click the **H** icon in the system tray to:
  - View current marks
  - Toggle "Run at startup"
  - See hotkey reference
  - Exit the application

### Persistence
Marks are saved to `%APPDATA%\Winharpoon\config.json` and persist across restarts.

## License

MIT
