using Microsoft.Win32;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace ClipHat;

internal sealed class SettingsWindow : Form
{
    private readonly History history;
    private readonly Action? quit;
    private readonly Panel content = new() { Dock = DockStyle.Fill, Padding = new Padding(26, 20, 26, 20), BackColor = Color.White };
    private readonly List<Button> tabs = [];
    private string currentPage = "General";
    public SettingsWindow(History history, Action? quit = null)
    {
        this.history = history; this.quit = quit;
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "ClipHat Settings"; ClientSize = new Size(660, 580); Font = new Font("Segoe UI", 10);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        BackColor = Color.FromArgb(244, 245, 247); Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        var nav = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 78, Padding = new Padding(110, 18, 0, 14), WrapContents = false };
        foreach (var name in new[] { "General", "History", "Privacy", "About" })
        {
            var button = new Button { Text = name, Width = 110, Height = 40, FlatStyle = FlatStyle.Flat, Margin = new Padding(0), TabStop = true };
            button.FlatAppearance.BorderSize = 0; button.Click += (_, _) => SelectPage(name); tabs.Add(button); nav.Controls.Add(button);
        }
        var surround = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 20) };
        surround.Controls.Add(content); Controls.Add(surround); Controls.Add(nav);
        content.Resize += (_, _) => { using var path = Theme.Rounded(new RectangleF(0, 0, content.Width, content.Height), 12); var old = content.Region; content.Region = new Region(path); old?.Dispose(); };
        FormClosed += (_, _) => history.Save();
        Shown += (_, _) => SelectPage(currentPage);
        DpiChanged += (_, _) => BeginInvoke(() => SelectPage(currentPage));
        ResumeLayout(true);
    }
    internal void SelectPage(string name)
    {
        currentPage = name;
        content.SuspendLayout();
        foreach (var tab in tabs) { tab.BackColor = tab.Text == name ? Color.White : BackColor; tab.ForeColor = tab.Text == name ? Theme.Blue : Theme.Ink; }
        foreach (Control control in content.Controls.Cast<Control>().ToArray()) control.Dispose();
        var page = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.White };
        page.SuspendLayout();
        var settings = history.Settings;
        switch (name)
        {
            case "General":
                Toggle(page, "Launch ClipHat at login", settings.LaunchAtLogin, value =>
                {
                    using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                    if (value) key.SetValue("ClipHat", '"' + Application.ExecutablePath + '"'); else key.DeleteValue("ClipHat", false);
                    settings.LaunchAtLogin = value;
                });
                Row(page, "Keyboard shortcut", new Label { Text = "Ctrl + Shift + V", AutoSize = true, ForeColor = Theme.Muted });
                Toggle(page, "Large image and document previews (2× cards)", settings.LargePreviews, x => settings.LargePreviews = x);
                Toggle(page, "Keep panel open (thumbtack)", settings.PanelPinned, x => settings.PanelPinned = x);
                var positions = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
                foreach (var side in new[] { "Left", "Right" })
                {
                    var option = new RadioButton { Text = side, Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, AutoSize = true, FlatStyle = FlatStyle.Flat, Checked = settings.PanelSide == side, Margin = new Padding(0, 0, 8, 0) };
                    option.CheckedChanged += (_, _) => { if (option.Checked) { settings.PanelSide = side; history.Save(); } }; positions.Controls.Add(option);
                }
                Row(page, "Panel position", positions);
                Note(page, "Select an item to copy it, then press Ctrl+V to paste.");
                Divider(page);
                var exit = ActionButton("Quit ClipHat", () => { Close(); quit?.Invoke(); }); exit.ForeColor = Color.Firebrick; page.Controls.Add(exit);
                break;
            case "History":
                var limit = new NumericUpDown { Minimum = 10, Maximum = 5000, Increment = 10, Value = Math.Clamp(settings.Limit, 10, 5000), Width = 92 };
                limit.ValueChanged += (_, _) => { settings.Limit = (int)limit.Value; history.Trim(); };
                Row(page, "Keep clipboard items", limit);
                Note(page, "Oldest unpinned items are removed first. Pinned items are always kept.");
                Heading(page, "Store");
                Toggle(page, "Text and links", settings.CaptureText, x => settings.CaptureText = x);
                Toggle(page, "Images", settings.CaptureImages, x => settings.CaptureImages = x);
                Toggle(page, "Documents and other files", settings.CaptureFiles, x => settings.CaptureFiles = x);
                Toggle(page, "Pause clipboard capture", settings.Paused, x => settings.Paused = x);
                Note(page, "Up to 1 MB per text item, 10 MB per image, and 20 MB per file. Folders are not supported.");
                Divider(page);
                var clearRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
                clearRow.Controls.Add(ActionButton("Clear Unpinned History", () => { if (Confirm("Delete all unpinned clipboard history?")) history.Clear(false); }));
                clearRow.Controls.Add(ActionButton("Clear All History…", () => { if (Confirm("Permanently delete all history, including pinned items?")) history.Clear(true); }));
                page.Controls.Add(clearRow);
                break;
            case "Privacy":
                Heading(page, "Ignored Apps");
                Note(page, "Clipboard activity from these applications is not saved.");
                var apps = new ListBox { Width = 550, Height = 136, BorderStyle = BorderStyle.FixedSingle, Font = Font };
                void Reload() { apps.Items.Clear(); foreach (var app in settings.IgnoredApps.OrderBy(x => x)) apps.Items.Add(app); }
                Reload(); page.Controls.Add(apps);
                var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 10, 0, 8) };
                actions.Controls.Add(ActionButton("Add Application…", () =>
                {
                    using var dialog = new OpenFileDialog { Title = "Ignore clipboard from an application", Filter = "Applications (*.exe)|*.exe" };
                    if (dialog.ShowDialog(this) == DialogResult.OK) { var app = Path.GetFileNameWithoutExtension(dialog.FileName); if (!settings.IgnoredApps.Contains(app, StringComparer.OrdinalIgnoreCase)) settings.IgnoredApps.Add(app); history.Save(); Reload(); }
                }));
                actions.Controls.Add(ActionButton("Remove", () => { if (apps.SelectedItem is string app) { settings.IgnoredApps.Remove(app); history.Save(); Reload(); } }));
                page.Controls.Add(actions);
                Note(page, "Everything stays on this PC. Sensitive clipboard markers are skipped. Apps may omit these markers; add any application you want excluded.");
                Note(page, "History is stored locally in Local AppData. Exclusions apply to future captures; clear existing history separately.");
                break;
            case "About":
                page.FlowDirection = FlowDirection.TopDown;
                var picture = new PictureBox { Width = 550, Height = 102, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 12, 0, 10), AccessibleName = "ClipHat app icon" };
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ClipHat.AppIcon.png") ?? throw new InvalidOperationException("ClipHat icon is missing."))
                using (var image = Image.FromStream(stream)) picture.Image = new Bitmap(image);
                picture.Disposed += (_, _) => picture.Image?.Dispose(); page.Controls.Add(picture);
                Center(page, "ClipHat", 22, true, Theme.Ink);
                Center(page, "Never lose something you copied.", 11, false, Theme.Ink);
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Center(page, $"Version {version?.ToString(3)} · Part of GuruHat", 10, false, Theme.Muted);
                Center(page, "Free & Open Source · MIT License", 9, false, Theme.Muted);
                Center(page, "No accounts. No cloud. No analytics.", 9, false, Theme.Muted);
                break;
        }
        // Pages are created after the form has already scaled; scale new controls
        // from their 96-DPI layout exactly once before attaching them.
        page.Scale(new SizeF(DeviceDpi / 96f, DeviceDpi / 96f));
        content.Controls.Add(page);
        page.ResumeLayout(true);
        content.ResumeLayout(true);
    }
    private bool Confirm(string text) => MessageBox.Show(this, text, "ClipHat", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    private static Button ActionButton(string title, Action action)
    {
        var button = new Button { Text = title, AutoSize = true, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(248, 249, 251), Padding = new Padding(5, 2, 5, 2), Margin = new Padding(0, 0, 10, 0) };
        button.FlatAppearance.BorderColor = Color.FromArgb(215, 220, 227); button.Click += (_, _) => action(); return button;
    }
    private void Toggle(Control parent, string title, bool value, Action<bool> change)
    {
        var toggle = new CheckBox { Text = title, Checked = value, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
        toggle.CheckedChanged += (_, _) =>
        {
            try { change(toggle.Checked); history.Save(); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException) { MessageBox.Show(this, e.Message, "ClipHat", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }; parent.Controls.Add(toggle);
    }
    private static void Row(Control parent, string title, Control value)
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 14) };
        row.Controls.Add(new Label { Text = title, Width = 260, Height = 26, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0) }); row.Controls.Add(value); parent.Controls.Add(row);
    }
    private static void Note(Control parent, string text) => parent.Controls.Add(new Label { Text = text, AutoSize = true, MaximumSize = new Size(550, 0), Font = new Font("Segoe UI", 9), ForeColor = Theme.Muted, Margin = new Padding(0, 0, 0, 12) });
    private static void Heading(Control parent, string text) => parent.Controls.Add(new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 0, 0, 10) });
    private static void Divider(Control parent) => parent.Controls.Add(new Panel { Width = 550, Height = 1, BackColor = Color.FromArgb(228, 232, 237), Margin = new Padding(0, 0, 0, 14) });
    private static void Center(Control parent, string text, float size, bool bold, Color color) => parent.Controls.Add(new Label { Text = text, Width = 550, Height = (int)(size * 2.3), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color, Margin = new Padding(0, 0, 0, 7) });
}
