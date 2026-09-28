using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace ClipHat;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(true, "GuruHat.ClipHat.Windows", out var first);
        if (!first) return;
        ApplicationConfiguration.Initialize();
        if (args.Contains("--smoke-test")) { PanelSmokeTest.Run(); return; }
        Application.Run(new ClipHatContext(args.Contains("--show-history")));
    }
}

internal sealed class ClipItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? Source { get; set; }
    public string Kind { get; set; } = "text";
    public string Text { get; set; } = "";
    public string? FileName { get; set; }
    public string? Payload { get; set; }
    public string Fingerprint { get; set; } = "";
    public DateTime CopiedAt { get; set; } = DateTime.UtcNow;
    public bool Pinned { get; set; }
}

internal sealed class Settings
{
    public int Limit { get; set; } = 500;
    public bool Paused { get; set; }
    public bool CaptureText { get; set; } = true;
    public bool CaptureImages { get; set; } = true;
    public bool CaptureFiles { get; set; } = true;
    public bool LaunchAtLogin { get; set; }
    public string PanelSide { get; set; } = "Left";
    public bool PanelPinned { get; set; }
    public bool LargePreviews { get; set; }
}

internal sealed class History
{
    private readonly string folder;
    private readonly string index;
    public List<ClipItem> Items { get; private set; } = [];
    public Settings Settings { get; private set; } = new();
    public History(string? storageFolder = null)
    {
        folder = storageFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GuruHat", "ClipHat");
        Directory.CreateDirectory(folder);
        index = Path.Combine(folder, "history.json");
        try { Items = JsonSerializer.Deserialize<List<ClipItem>>(File.ReadAllText(index)) ?? []; } catch (IOException) { } catch (JsonException) { }
        try { Settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path.Combine(folder, "settings.json"))) ?? new(); } catch (IOException) { } catch (JsonException) { }
    }
    public string SavePayload(byte[] bytes, string name)
    {
        var path = Path.Combine(folder, "payloads", Guid.NewGuid().ToString("N") + Path.GetExtension(name));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        return path;
    }
    public void Add(ClipItem item)
    {
        var old = Items.FirstOrDefault(x => x.Fingerprint == item.Fingerprint);
        if (old != null)
        {
            item.Id = old.Id;
            item.Pinned = old.Pinned;
            if (item.Payload == null) item.Payload = old.Payload;
            else if (old.Payload != item.Payload) DeletePayload(old);
            Items.Remove(old);
        }
        Items.Insert(0, item);
        Trim();
        Save();
    }
    public void Trim()
    {
        var keep = Math.Clamp(Settings.Limit, 10, 5000);
        foreach (var item in Items.Where(x => !x.Pinned).Skip(Math.Max(0, keep - Items.Count(x => x.Pinned))).ToArray()) Remove(item, false);
        Save();
    }
    public void Remove(ClipItem item, bool save = true)
    {
        Items.Remove(item);
        DeletePayload(item);
        if (save) Save();
    }
    private static void DeletePayload(ClipItem item)
    {
        if (item.Payload != null) try { File.Delete(item.Payload); } catch (IOException) { }
    }
    public void Clear(bool includePinned)
    {
        foreach (var item in Items.Where(x => includePinned || !x.Pinned).ToArray()) Remove(item, false);
        Save();
    }
    public void Save()
    {
        var temp = index + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(Items));
        File.Move(temp, index, true);
        File.WriteAllText(Path.Combine(folder, "settings.json"), JsonSerializer.Serialize(Settings));
    }
}

internal sealed class ClipHatContext : ApplicationContext
{
    private readonly History history = new();
    private readonly MainWindow window;
    private readonly NotifyIcon tray;
    private readonly Timer timer;
    private uint sequence;
    private bool restoring;
    private const int Hotkey = 0x4231;
    public ClipHatContext(bool showHistory = false)
    {
        window = new MainWindow(history, Restore, OpenSettings, Quit);
        window.FormClosing += (_, e) => { if (e.CloseReason != CloseReason.ApplicationExitCall) { e.Cancel = true; window.ClosePanel(); } };
        window.HotkeyPressed += () => { if (window.Visible) window.ClosePanel(); else Show(); };
        tray = new NotifyIcon { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath), Text = "ClipHat — Ctrl+Shift+V", Visible = true };
        tray.DoubleClick += (_, _) => Show();
        tray.ContextMenuStrip = new ContextMenuStrip();
        tray.ContextMenuStrip.Items.Add("Open history", null, (_, _) => Show());
        tray.ContextMenuStrip.Items.Add("Pause capture", null, (_, _) => TogglePause());
        tray.ContextMenuStrip.Items.Add("Settings", null, (_, _) => OpenSettings());
        tray.ContextMenuStrip.Items.Add("Quit", null, (_, _) => Quit());
        timer = new Timer { Interval = 750 };
        timer.Tick += (_, _) => Poll();
        sequence = Native.GetClipboardSequenceNumber();
        window.EnsureHotkey(Hotkey);
        timer.Start();
        if (showHistory) Show();
    }
    private void Show()
    {
        window.RefreshItems();
        window.OpenPanel(Screen.FromPoint(Cursor.Position), history.Settings.PanelSide);
    }
    private void TogglePause() { history.Settings.Paused = !history.Settings.Paused; history.Save(); window.RefreshItems(); }
    private void Poll()
    {
        var next = Native.GetClipboardSequenceNumber();
        if (next == sequence) return;
        sequence = next;
        if (restoring || history.Settings.Paused) return;
        try
        {
            var data = Clipboard.GetDataObject();
            if (data == null) return;
            var formats = data.GetFormats();
            if (formats.Any(x => x.Contains("ExcludeClipboardContentFromMonitorProcessing", StringComparison.OrdinalIgnoreCase) || x.Contains("CanIncludeInClipboardHistory", StringComparison.OrdinalIgnoreCase))) return;
            ClipItem? item = null;
            byte[]? payload = null;
            if (history.Settings.CaptureFiles && data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = data.GetData(DataFormats.FileDrop) as string[];
                var path = paths?.FirstOrDefault(File.Exists);
                if (path != null && new FileInfo(path).Length <= 20 * 1024 * 1024)
                {
                    payload = File.ReadAllBytes(path);
                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    var kind = new[] { ".mp3", ".wav", ".m4a", ".flac", ".ogg", ".wma" }.Contains(ext) ? "audio" : "document";
                    item = new ClipItem { Kind = kind, Text = path, FileName = Path.GetFileName(path) };
                }
            }
            else if (history.Settings.CaptureImages && data.GetDataPresent(DataFormats.Bitmap))
            {
                using var image = data.GetData(DataFormats.Bitmap) as Image;
                if (image != null)
                {
                    using var stream = new MemoryStream();
                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    if (stream.Length <= 10 * 1024 * 1024) { payload = stream.ToArray(); item = new ClipItem { Kind = "image", Text = $"Image {image.Width} × {image.Height}", FileName = "Image.png" }; }
                }
            }
            else if (history.Settings.CaptureText && data.GetDataPresent(DataFormats.UnicodeText))
            {
                var value = data.GetData(DataFormats.UnicodeText) as string;
                if (!string.IsNullOrWhiteSpace(value) && Encoding.UTF8.GetByteCount(value) <= 1024 * 1024)
                    item = new ClipItem { Kind = Uri.TryCreate(value, UriKind.Absolute, out var url) && (url.Scheme == "https" || url.Scheme == "http") ? "link" : "text", Text = value };
            }
            if (item == null) return;
            item.Source = ClipboardSource();
            item.Fingerprint = Convert.ToHexString(SHA256.HashData(payload ?? Encoding.UTF8.GetBytes(item.Kind + ":" + item.Text)));
            if (payload != null && !history.Items.Any(x => x.Fingerprint == item.Fingerprint)) item.Payload = history.SavePayload(payload, item.FileName!);
            history.Add(item);
            window.RefreshItems();
        }
        catch (Exception e) when (e is ExternalException or IOException or UnauthorizedAccessException or ArgumentException) { }
    }
    private static string? ClipboardSource()
    {
        try
        {
            var handle = Native.GetClipboardOwner();
            if (handle == IntPtr.Zero) handle = Native.GetForegroundWindow();
            Native.GetWindowThreadProcessId(handle, out var processId);
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { return null; }
    }
    private void Restore(ClipItem item)
    {
        try
        {
            restoring = true;
            if (item.Kind == "image" && item.Payload != null) { using var image = Image.FromFile(item.Payload); Clipboard.SetImage(new Bitmap(image)); }
            else if ((item.Kind == "document" || item.Kind == "audio") && item.Payload != null)
            {
                var exported = Path.Combine(Path.GetTempPath(), "ClipHat", Guid.NewGuid().ToString("N"), item.FileName ?? "file");
                Directory.CreateDirectory(Path.GetDirectoryName(exported)!);
                File.Copy(item.Payload, exported);
                Clipboard.SetFileDropList(new StringCollection { exported });
            }
            else Clipboard.SetText(item.Text);
            sequence = Native.GetClipboardSequenceNumber();
            if (!history.Settings.PanelPinned) window.ClosePanel();
        }
        catch (Exception e) when (e is ExternalException or IOException or UnauthorizedAccessException) { MessageBox.Show(e.Message, "ClipHat", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { restoring = false; }
    }
    private void OpenSettings()
    {
        using var settings = new SettingsWindow(history);
        settings.ShowDialog();
        history.Trim(); window.RefreshItems();
    }
    private void Quit() { timer.Stop(); window.ReleaseHotkey(Hotkey); tray.Visible = false; tray.Dispose(); window.Dispose(); ExitThread(); }
}

internal static class Native
{
    [DllImport("user32.dll")] public static extern IntPtr GetClipboardOwner();
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern uint GetClipboardSequenceNumber();
    [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint key);
    [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr handle, int id);
}
