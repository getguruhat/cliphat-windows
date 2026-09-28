using System.Diagnostics;
using Microsoft.Win32;

namespace ClipHat;

internal sealed class MainWindow : Form
{
    private readonly History history;
    private readonly Action<ClipItem> restore;
    private readonly Action settings;
    private readonly Action quit;
    private readonly TextBox search = new() { PlaceholderText = "Search clipboard history", Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
    private readonly FlowLayoutPanel cards = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.FromArgb(247, 248, 251) };
    private readonly ComboBox filter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 108 };
    private readonly Label count = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly System.Windows.Forms.Timer slideTimer = new() { Interval = 15 };
    private Rectangle edgeArea;
    private string panelSide = "Left";
    private int slideStart;
    private int slideEnd;
    private long slideStarted;
    private bool closing;
    private int hotkey;
    public event Action? HotkeyPressed;
    public MainWindow(History history, Action<ClipItem> restore, Action settings, Action quit)
    {
        this.history = history; this.restore = restore; this.settings = settings; this.quit = quit;
        Text = "ClipHat"; Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Size = new Size(350, 690); MinimumSize = new Size(300, 430); StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None; TopMost = true;
        ShowInTaskbar = false; KeyPreview = true; BackColor = Color.White;
        var title = new Label { Text = "ClipHat", Font = new Font("Segoe UI", 19, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(34, 46, 64), Margin = new Padding(0, 0, 0, 10) };
        var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 116, Padding = new Padding(16, 13, 16, 10), ColumnCount = 3, RowCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(title, 0, 0);
        var menu = new Button { Text = "⚙", Width = 38, Height = 34, FlatStyle = FlatStyle.Flat, TabStop = false };
        menu.FlatAppearance.BorderSize = 0; menu.Click += (_, _) => settings(); header.Controls.Add(menu, 1, 0);
        var close = new Button { Text = "×", Width = 34, Height = 34, FlatStyle = FlatStyle.Flat, TabStop = false, Font = new Font("Segoe UI", 15) };
        close.FlatAppearance.BorderSize = 0; close.Click += (_, _) => ClosePanel(); header.Controls.Add(close, 2, 0);
        header.SetColumnSpan(search, 3); header.Controls.Add(search, 0, 1);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(15, 6, 12, 4), WrapContents = false };
        filter.Items.AddRange(["All", "Text", "Links", "Images", "Documents", "Audio"]); filter.SelectedIndex = 0;
        toolbar.Controls.Add(filter);
        var clear = new Button { Text = "Clear", Width = 60, Height = 25, FlatStyle = FlatStyle.Flat };
        clear.Click += (_, _) => { if (MessageBox.Show("Delete all unpinned history?", "ClipHat", MessageBoxButtons.YesNo) == DialogResult.Yes) { history.Clear(false); RefreshItems(); } };
        toolbar.Controls.Add(clear); toolbar.Controls.Add(count);
        var footer = new Label { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(15, 7, 0, 0), Text = "Ctrl+Shift+V to open  •  Enter to copy  •  Esc to close", ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8.5f) };
        Controls.Add(cards); Controls.Add(footer); Controls.Add(toolbar); Controls.Add(header);
        search.TextChanged += (_, _) => RefreshItems(); filter.SelectedIndexChanged += (_, _) => RefreshItems();
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) ClosePanel(); if (e.Control && e.KeyCode == Keys.F) FocusSearch(); };
        search.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { var first = VisibleItems().FirstOrDefault(); if (first != null) restore(first); e.SuppressKeyPress = true; } };
        cards.Resize += (_, _) => ResizeCards();
        slideTimer.Tick += (_, _) => AdvanceSlide();
        Deactivate += (_, _) => { if (Visible && !closing) BeginInvoke(() => { if (!ContainsFocus) ClosePanel(); }); };
        RefreshItems();
    }
    public void OpenPanel(Screen screen, string side)
    {
        slideTimer.Stop(); closing = false;
        edgeArea = screen.WorkingArea;
        panelSide = side == "Right" ? "Right" : "Left";
        var width = Math.Clamp((int)(edgeArea.Width * .19), 300, 350);
        Bounds = new Rectangle(OffscreenX(width), edgeArea.Top, width, edgeArea.Height);
        slideStart = Left; slideEnd = EdgeX(); slideStarted = Environment.TickCount64;
        Show(); Activate(); FocusSearch(); slideTimer.Start();
    }
    public void ClosePanel()
    {
        if (!Visible || closing) return;
        closing = true; slideTimer.Stop();
        slideStart = Left; slideEnd = OffscreenX(Width); slideStarted = Environment.TickCount64;
        slideTimer.Start();
    }
    private int EdgeX() => panelSide == "Right" ? edgeArea.Right - Width : edgeArea.Left;
    private int OffscreenX(int width) => panelSide == "Right" ? edgeArea.Right : edgeArea.Left - width;
    private void AdvanceSlide()
    {
        var duration = closing ? 180.0 : 220.0;
        var progress = Math.Clamp((Environment.TickCount64 - slideStarted) / duration, 0, 1);
        var eased = closing ? progress * progress : 1 - Math.Pow(1 - progress, 3);
        Left = slideStart + (int)Math.Round((slideEnd - slideStart) * eased);
        if (progress < 1) return;
        slideTimer.Stop(); Left = slideEnd;
        if (closing) Hide();
    }
    public void FocusSearch() { search.Focus(); search.SelectAll(); }
    public void EnsureHotkey(int id) { hotkey = id; Native.RegisterHotKey(Handle, id, 0x0002 | 0x0004, (uint)Keys.V); }
    public void ReleaseHotkey(int id) { Native.UnregisterHotKey(Handle, id); }
    protected override void WndProc(ref Message m) { if (m.Msg == 0x0312 && m.WParam.ToInt32() == hotkey) HotkeyPressed?.Invoke(); base.WndProc(ref m); }
    private IEnumerable<ClipItem> VisibleItems() => history.Items.Where(x => (filter.SelectedIndex <= 0 || x.Kind == new[] { "", "text", "link", "image", "document", "audio" }[filter.SelectedIndex]) && (search.Text.Length == 0 || x.Text.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase) || (x.FileName?.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase) ?? false)));
    public void RefreshItems()
    {
        if (IsDisposed) return;
        cards.SuspendLayout(); cards.Controls.Clear();
        var items = VisibleItems().ToArray();
        foreach (var item in items)
        {
            var card = new Panel { Width = Math.Max(290, cards.ClientSize.Width - 30), Height = item.Kind == "image" ? 130 : 89, BackColor = Color.White, Margin = new Padding(9, 5, 9, 5), BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
            var kind = new Label { Text = item.Kind.ToUpperInvariant() + "  ·  " + item.CopiedAt.ToLocalTime().ToString("g"), Location = new Point(12, 8), Width = card.Width - 85, Height = 18, ForeColor = Color.SteelBlue, Font = new Font("Segoe UI", 8, FontStyle.Bold) };
            var preview = new Label { Text = item.FileName ?? item.Text, Location = new Point(12, 29), Size = new Size(card.Width - 65, item.Kind == "image" ? 88 : 48), AutoEllipsis = true, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(30, 38, 50) };
            if (item.Kind == "image" && item.Payload != null && File.Exists(item.Payload))
            {
                try { using var img = Image.FromFile(item.Payload); var bitmap = new Bitmap(img); var pic = new PictureBox { Image = bitmap, SizeMode = PictureBoxSizeMode.Zoom, Location = new Point(12, 29), Size = new Size(card.Width - 65, 88) }; card.Controls.Add(pic); pic.Click += (_, _) => restore(item); pic.Disposed += (_, _) => bitmap.Dispose(); }
                catch (IOException) { card.Controls.Add(preview); }
            }
            else card.Controls.Add(preview);
            var pin = new Button { Text = item.Pinned ? "★" : "☆", Location = new Point(card.Width - 47, 5), Size = new Size(28, 25), FlatStyle = FlatStyle.Flat, ForeColor = Color.DarkGoldenrod };
            pin.FlatAppearance.BorderSize = 0; pin.Click += (_, _) => { item.Pinned = !item.Pinned; history.Save(); RefreshItems(); };
            var context = new ContextMenuStrip();
            context.Items.Add("Copy", null, (_, _) => restore(item));
            if (item.Kind == "link") context.Items.Add("Open link", null, (_, _) => { try { Process.Start(new ProcessStartInfo(item.Text) { UseShellExecute = true }); } catch (Exception) { } });
            context.Items.Add(item.Pinned ? "Unpin" : "Pin", null, (_, _) => { item.Pinned = !item.Pinned; history.Save(); RefreshItems(); });
            context.Items.Add("Delete", null, (_, _) => { history.Remove(item); RefreshItems(); });
            card.ContextMenuStrip = context;
            card.Controls.Add(kind); card.Controls.Add(pin);
            card.Click += (_, _) => restore(item); kind.Click += (_, _) => restore(item); preview.Click += (_, _) => restore(item);
            cards.Controls.Add(card);
        }
        count.Text = $"  {items.Length} items" + (history.Settings.Paused ? " · Paused" : "");
        cards.ResumeLayout();
    }
    private void ResizeCards() { foreach (Control control in cards.Controls) control.Width = Math.Max(290, cards.ClientSize.Width - 30); }
}

internal sealed class SettingsWindow : Form
{
    public SettingsWindow(History history)
    {
        Text = "ClipHat Settings"; Size = new Size(420, 390); StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        var settings = history.Settings;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20), AutoScroll = true };
        var limitLabel = new Label { Text = "Keep clipboard items", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(3, 0, 0, 4) };
        var limit = new NumericUpDown { Minimum = 10, Maximum = 5000, Increment = 10, Value = Math.Clamp(settings.Limit, 10, 5000), Width = 140 };
        limit.ValueChanged += (_, _) => { settings.Limit = (int)limit.Value; history.Trim(); };
        layout.Controls.Add(limitLabel); layout.Controls.Add(limit);
        var sideLabel = new Label { Text = "Panel side", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(3, 12, 0, 4) };
        var side = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        side.Items.AddRange(["Left", "Right"]); side.SelectedItem = settings.PanelSide == "Right" ? "Right" : "Left";
        side.SelectedIndexChanged += (_, _) => { settings.PanelSide = side.SelectedItem?.ToString() ?? "Left"; history.Save(); };
        layout.Controls.Add(sideLabel); layout.Controls.Add(side);
        AddToggle("Pause clipboard capture", settings.Paused, x => settings.Paused = x);
        AddToggle("Capture text and links", settings.CaptureText, x => settings.CaptureText = x);
        AddToggle("Capture images", settings.CaptureImages, x => settings.CaptureImages = x);
        AddToggle("Capture files", settings.CaptureFiles, x => settings.CaptureFiles = x);
        AddToggle("Launch at login", settings.LaunchAtLogin, x =>
        {
            try { using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true); if (x) key?.SetValue("ClipHat", '"' + Application.ExecutablePath + '"'); else key?.DeleteValue("ClipHat", false); settings.LaunchAtLogin = x; }
            catch (Exception e) { MessageBox.Show(e.Message, "ClipHat", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        });
        var clear = new Button { Text = "Clear all history, including pins", AutoSize = true, Margin = new Padding(3, 12, 0, 0) };
        clear.Click += (_, _) => { if (MessageBox.Show("Permanently delete all clipboard history?", "ClipHat", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) history.Clear(true); };
        layout.Controls.Add(clear);
        var note = new Label { Text = "History stays on this PC in Local AppData.\nShortcut: Ctrl+Shift+V", AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(3, 16, 0, 0) };
        layout.Controls.Add(note);
        Controls.Add(layout);
        FormClosed += (_, _) => history.Save();
        void AddToggle(string title, bool value, Action<bool> change)
        {
            var toggle = new CheckBox { Text = title, Checked = value, AutoSize = true, Margin = new Padding(3, 10, 0, 0) };
            toggle.CheckedChanged += (_, _) => { change(toggle.Checked); history.Save(); };
            layout.Controls.Add(toggle);
        }
    }
}
