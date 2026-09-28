using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ClipHat;

internal sealed class MainWindow : Form
{
    private readonly History history;
    private readonly Action<ClipItem> restore;
    private readonly TextBox search = new() { PlaceholderText = "Search clipboard…", BorderStyle = BorderStyle.None };
    private readonly FlowLayoutPanel cards = new() { AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly Panel header = new(), footer = new();
    private readonly Label count = new() { TextAlign = ContentAlignment.MiddleLeft };
    private readonly List<IconButton> filters = [];
    private readonly IconButton previewButton, pauseButton, tackButton;
    private readonly ToolTip tips = new();
    private readonly System.Windows.Forms.Timer slideTimer = new() { Interval = 15 };
    private Rectangle edgeArea;
    private string panelSide = "Left", filter = "all";
    private string? selected;
    private int slideStart, slideEnd, hotkey;
    private long slideStarted;
    private bool closing, searchVisible, refreshing;
    private float ScaleFactor => DeviceDpi / 96f;
    private int Px(float value) => (int)Math.Round(value * ScaleFactor);
    public bool PanelPinned => history.Settings.PanelPinned;
    public event Action? HotkeyPressed;

    public MainWindow(History history, Action<ClipItem> restore, Action settings, Action quit)
    {
        this.history = history; this.restore = restore;
        Text = "ClipHat"; Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        AutoScaleMode = AutoScaleMode.None;
        Size = new Size(350, 690); StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None; TopMost = true; ShowInTaskbar = false;
        KeyPreview = true; DoubleBuffered = true; BackColor = Theme.Panel;
        cards.BackColor = Theme.Panel;
        search.Font = new Font("Segoe UI", 11); search.BackColor = Theme.Panel; search.Visible = false;
        count.Font = new Font("Segoe UI", 9); count.ForeColor = Theme.Ink;
        Controls.AddRange([cards, header, footer, search]);
        AddButton(header, "search", "Search (Ctrl+F)", () => { searchVisible = !searchVisible; Arrange(); if (searchVisible) FocusSearch(); });
        foreach (var (kind, label) in new[] { ("all", "All items"), ("text", "Text"), ("link", "Links"), ("image", "Images"), ("document", "Documents"), ("audio", "Audio") })
        {
            var button = AddButton(header, kind, label, () => { filter = kind; RefreshItems(); });
            filters.Add(button);
        }
        footer.Controls.Add(count);
        previewButton = AddButton(footer, "expand", "Large image and file previews", () => { history.Settings.LargePreviews = !history.Settings.LargePreviews; SaveAndRefresh(); });
        pauseButton = AddButton(footer, "pause", "Pause clipboard capture", () => { history.Settings.Paused = !history.Settings.Paused; SaveAndRefresh(); });
        tackButton = AddButton(footer, "pin", "Keep panel open", TogglePanelPin);
        AddButton(footer, "gear", "Settings", () => { ClosePanel(); settings(); });
        search.TextChanged += (_, _) => RefreshItems();
        cards.Resize += (_, _) => ResizeCards();
        slideTimer.Tick += (_, _) => AdvanceSlide();
        Deactivate += (_, _) => { if (Visible && !closing) BeginInvoke(() => { if (!ContainsFocus && !PanelPinned) ClosePanel(); }); };
        Resize += (_, _) => Arrange();
        DpiChanged += (_, _) => { Arrange(); RefreshItems(); };
        Arrange(); RefreshItems();
    }
    private IconButton AddButton(Control parent, string icon, string label, Action action)
    {
        var button = new IconButton(icon) { AccessibleName = label, AccessibleRole = AccessibleRole.PushButton };
        button.Click += (_, _) => action(); tips.SetToolTip(button, label); parent.Controls.Add(button); return button;
    }
    private void Arrange()
    {
        var s = ScaleFactor;
        header.SetBounds(Px(12), Px(5), Width - Px(24), Px(34));
        for (var i = 0; i < header.Controls.Count; i++) header.Controls[i].SetBounds(i * header.Width / 7, 0, header.Width / 7 - Px(2), Px(32));
        search.Visible = searchVisible;
        search.SetBounds(Px(20), Px(47), Width - Px(40), Px(25));
        var top = Px(searchVisible ? 80 : 44);
        footer.SetBounds(0, Height - Px(39), Width, Px(39));
        count.SetBounds(Px(16), 0, Math.Max(0, Width - Px(176)), Px(39));
        for (var i = 1; i < footer.Controls.Count; i++) footer.Controls[i].SetBounds(Width - Px(152) + (i - 1) * Px(34), Px(3), Px(32), Px(32));
        cards.SetBounds(0, top, Width, Math.Max(0, footer.Top - top));
        var old = Region;
        if (Width > 0 && Height > 0) { using var shape = Theme.Rounded(new RectangleF(0, 0, Width, Height), Px(17)); Region = new Region(shape); old?.Dispose(); }
        ResizeCards(); Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); using var pen = new Pen(Color.FromArgb(181, 202, 218));
        e.Graphics.DrawLine(pen, 0, cards.Top - 1, Width, cards.Top - 1);
        e.Graphics.DrawLine(pen, 0, footer.Top, Width, footer.Top);
    }
    public void TogglePanelPin() { history.Settings.PanelPinned = !PanelPinned; SaveAndRefresh(); }
    private void SaveAndRefresh() { history.Save(); RefreshItems(); }
    public void OpenPanel(Screen screen, string side)
    {
        slideTimer.Stop(); closing = false; edgeArea = screen.WorkingArea;
        panelSide = side == "Right" ? "Right" : "Left";
        var width = Math.Min(edgeArea.Width, Px(350));
        Bounds = new Rectangle(OffscreenX(width), edgeArea.Top, width, edgeArea.Height);
        RefreshItems();
        slideStart = Left; slideEnd = panelSide == "Right" ? edgeArea.Right - Width : edgeArea.Left; slideStarted = Environment.TickCount64;
        Show(); Activate(); if (searchVisible) search.Focus(); else Focus(); slideTimer.Start();
    }
    public void ClosePanel()
    {
        if (!Visible || closing) return;
        closing = true; slideTimer.Stop(); slideStart = Left; slideEnd = OffscreenX(Width); slideStarted = Environment.TickCount64; slideTimer.Start();
    }
    private int OffscreenX(int width) => panelSide == "Right" ? edgeArea.Right : edgeArea.Left - width;
    private void AdvanceSlide()
    {
        var progress = Math.Clamp((Environment.TickCount64 - slideStarted) / (closing ? 180.0 : 220.0), 0, 1);
        var eased = closing ? progress * progress : 1 - Math.Pow(1 - progress, 3);
        Left = slideStart + (int)Math.Round((slideEnd - slideStart) * eased);
        if (progress < 1) return;
        slideTimer.Stop(); Left = slideEnd; if (closing) Hide();
    }
    public void FocusSearch() { searchVisible = true; Arrange(); search.Focus(); search.SelectAll(); }
    public void EnsureHotkey(int id) { hotkey = id; Native.RegisterHotKey(Handle, id, 0x0002 | 0x0004, (uint)Keys.V); }
    public void ReleaseHotkey(int id) { Native.UnregisterHotKey(Handle, id); }
    protected override void WndProc(ref Message m) { if (m.Msg == 0x0312 && m.WParam.ToInt32() == hotkey) HotkeyPressed?.Invoke(); base.WndProc(ref m); }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { ClosePanel(); return true; }
        if (keyData == (Keys.Control | Keys.F)) { FocusSearch(); return true; }
        var items = VisibleItems().ToArray();
        if (keyData == Keys.Down || keyData == Keys.Up)
        {
            if (items.Length == 0) return true;
            var index = Array.FindIndex(items, x => x.Id == selected);
            selected = items[Math.Clamp(index + (keyData == Keys.Down ? 1 : -1), 0, items.Length - 1)].Id;
            foreach (var card in cards.Controls.OfType<HistoryCard>()) { card.Selected = card.Item.Id == selected; if (card.Selected) cards.ScrollControlIntoView(card); }
            return true;
        }
        var current = items.FirstOrDefault(x => x.Id == selected);
        if (keyData == Keys.Enter && current != null) { restore(current); return true; }
        if (keyData == (Keys.Control | Keys.Delete) && current != null) { history.Remove(current); RefreshItems(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    private IEnumerable<ClipItem> VisibleItems() => history.Items.Where(x => (filter == "all" || x.Kind == filter) && (search.Text.Length == 0 || x.Text.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase) || (x.FileName?.Contains(search.Text, StringComparison.CurrentCultureIgnoreCase) ?? false)));
    public void RefreshItems()
    {
        if (IsDisposed || refreshing) return;
        refreshing = true; cards.SuspendLayout();
        var scroll = cards.AutoScrollPosition;
        foreach (Control control in cards.Controls.Cast<Control>().ToArray()) control.Dispose();
        var items = VisibleItems().ToArray();
        if (!items.Any(x => x.Id == selected)) selected = items.FirstOrDefault()?.Id;
        DateTime? lastDate = null;
        foreach (var item in items)
        {
            var date = item.CopiedAt.ToLocalTime().Date;
            if (date != lastDate)
            {
                var title = date == DateTime.Today ? "Today" : date == DateTime.Today.AddDays(-1) ? "Yesterday" : date.ToString("MMMM d");
                cards.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleLeft, Height = Px(35), Margin = new Padding(Px(22), Px(4), 0, 0) });
                lastDate = date;
            }
            var card = new HistoryCard(item, history.Settings.LargePreviews) { Selected = item.Id == selected, Margin = new Padding(Px(11), Px(5), Px(11), Px(5)) };
            card.Restore += () => restore(item);
            card.Delete += () => { history.Remove(item); RefreshItems(); };
            card.Pin += () => { item.Pinned = !item.Pinned; SaveAndRefresh(); };
            tips.SetToolTip(card, item.FileName != null ? item.Text : "Click to copy • Right-click for more options");
            cards.Controls.Add(card);
        }
        if (items.Length == 0) cards.Controls.Add(new Label { Text = search.Text.Length == 0 ? "Never lose something you copied.\n\nCopy text, a link, an image, or a file to begin." : "No matching items", ForeColor = Theme.Muted, Font = new Font("Segoe UI", 10), Height = Px(150), TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(Px(15)) });
        count.Text = $"{items.Length} item{(items.Length == 1 ? "" : "s")}";
        foreach (var button in filters) button.Active = button.IconName == filter;
        previewButton.Active = history.Settings.LargePreviews;
        pauseButton.IconName = history.Settings.Paused ? "play" : "pause"; pauseButton.Active = history.Settings.Paused;
        tackButton.Active = PanelPinned;
        tips.SetToolTip(tackButton, PanelPinned ? "Unpin panel — allow auto-close" : "Keep panel open");
        tips.SetToolTip(pauseButton, history.Settings.Paused ? "Resume clipboard capture" : "Pause clipboard capture");
        ResizeCards(); cards.ResumeLayout(); cards.AutoScrollPosition = new Point(-scroll.X, -scroll.Y); refreshing = false;
    }
    private void ResizeCards()
    {
        foreach (Control control in cards.Controls) control.Width = Math.Max(Px(180), cards.ClientSize.Width - Px(28) - SystemInformation.VerticalScrollBarWidth);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { slideTimer.Dispose(); tips.Dispose(); }
        base.Dispose(disposing);
    }
}
