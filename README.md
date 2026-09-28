# ClipHat for Windows

**Your clipboard, always at hand.**

ClipHat is a native Windows clipboard history app from GuruHat. It stores copied text, links, images, and files locally so you can find and restore them later. It runs in the notification area and opens with **Ctrl+Shift+V**.

## Install the latest build

Download the ready-to-install package: [ClipHat-1.0.0-Windows-Setup.exe](https://github.com/getguruhat/cliphat-windows/releases/download/v1.0.0-windows/ClipHat-1.0.0-Windows-Setup.exe).

Run the installer on Windows 10 or 11 (x64), then launch ClipHat. Windows may show a SmartScreen warning because this community build is not code signed.

## Use

- Copy text, links, images, or a regular file, then press **Ctrl+Shift+V** to open history.
- Search or filter by type, and click an item or press **Enter** to put it back on the clipboard. Press **Ctrl+V** in the target app to paste.
- Use the star to pin an item. Right-click a card to copy, pin, delete, or open a link.
- Use Settings to pause capture, select content types, set retention, clear history, or launch at login.

ClipHat stores history in `%LOCALAPPDATA%\GuruHat\ClipHat`. There are no accounts, cloud services, analytics, or network calls. Text is limited to 1 MB, images to 10 MB, and individual files to 20 MB. Copied folders are skipped.

## Build

The source is a .NET 8 Windows Forms project. On Windows with the .NET 8 SDK:

```powershell
dotnet run --project ClipHat.csproj
```

GitHub Actions publishes a self-contained x64 build, packages it with Inno Setup, and attaches the installer to the [Windows release](https://github.com/getguruhat/cliphat-windows/releases/tag/v1.0.0-windows).

The macOS version is available at [cliphat-macos](https://github.com/getguruhat/cliphat-macos).
