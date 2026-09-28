# ClipHat for Windows

**Your clipboard, always at hand.**

ClipHat is a native Windows clipboard history app from GuruHat. It stores copied text, links, images, and files locally so you can find and restore them later. It runs in the notification area and opens with **Ctrl+Shift+V**.

## Install the latest build

Download the ready-to-install package: [ClipHat-1.2.0-Windows-Setup.exe](https://github.com/getguruhat/cliphat-windows/releases/download/v1.2.0-windows/ClipHat-1.2.0-Windows-Setup.exe).

Run the installer on Windows 10 or 11 (x64), then launch ClipHat. Windows may show a SmartScreen warning because this community build is not code signed.

When upgrading, quit ClipHat from its notification-area menu, run the new installer, and launch it again. The sliding panel has no Windows title bar. Settings displays the installed version.

## Use

- Copy text, links, images, or a regular file, then press **Ctrl+Shift+V** to slide history in from the side of the active screen. Choose the left or right edge in Settings.
- Use the icon row to search or filter by type. Rounded white cards show previews, source apps, and copy times.
- Use the bottom toolbar to enlarge image/file previews, pause capture, keep the panel open with the thumbtack, or open Settings. A blue thumbtack keeps the panel visible while working in other apps; click it again to restore auto-close. **Esc** and **Ctrl+Shift+V** still close the panel.
- Drag cards into another app to transfer text or real image/file copies.
- Use **Up/Down** to select a card, **Enter** to copy, **Ctrl+F** to search, and **Ctrl+Delete** to delete.
- Search or filter by type, and click an item or press **Enter** to put it back on the clipboard. Press **Ctrl+V** in the target app to paste.
- Right-click a card to copy, pin/unpin an item, delete it, or open a link. Item pins protect entries from retention; the toolbar thumbtack controls whether the panel stays open. The red × on each card deletes that entry.
- Use Settings to pause capture, select content types, set retention, clear history, or launch at login.

ClipHat stores history in `%LOCALAPPDATA%\GuruHat\ClipHat`. There are no accounts, cloud services, analytics, or network calls. Text is limited to 1 MB, images to 10 MB, and individual files to 20 MB. Copied folders are skipped.

## Build

The source is a .NET 8 Windows Forms project. On Windows with the .NET 8 SDK:

```powershell
dotnet run --project ClipHat.csproj
```

GitHub Actions publishes a self-contained x64 build, packages it with Inno Setup, and attaches the installer to the [Windows release](https://github.com/getguruhat/cliphat-windows/releases/tag/v1.2.0-windows).

The macOS version is available at [cliphat-macos](https://github.com/getguruhat/cliphat-macos).
