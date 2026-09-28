using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ClipHat;

internal static class Theme
{
    public static readonly Color Panel = Color.FromArgb(216, 232, 245), Ink = Color.FromArgb(36, 42, 48), Muted = Color.FromArgb(114, 130, 142), Blue = Color.FromArgb(0, 122, 255);
    public static GraphicsPath Rounded(RectangleF r, float radius)
    {
        var p = new GraphicsPath(); float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { p.AddRectangle(r); return p; }
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
    }
    public static void Icon(Graphics g, string name, RectangleF bounds, Color color)
    {
        var state = g.Save(); g.TranslateTransform(bounds.X, bounds.Y); g.ScaleTransform(bounds.Width / 24f, bounds.Height / 24f);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var p = new Pen(color, 1.65f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(color);
        switch (name)
        {
            case "search": g.DrawEllipse(p, 3, 3, 12, 12); g.DrawLine(p, 14, 14, 21, 21); break;
            case "all": foreach (var x in new[] { 3, 14 }) foreach (var y in new[] { 3, 14 }) g.DrawRectangle(p, x, y, 7, 7); break;
            case "text": using (var f = new Font("Segoe UI", 19, FontStyle.Regular, GraphicsUnit.Pixel)) g.DrawString("Aa", f, brush, -1, 0); break;
            case "link":
                g.RotateTransform(-40, MatrixOrder.Prepend);
                g.ResetTransform(); g.Restore(state); state = g.Save();
                g.TranslateTransform(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2); g.ScaleTransform(bounds.Width / 24, bounds.Height / 24); g.RotateTransform(-40);
                using (var a = Rounded(new RectangleF(-5, -11, 10, 13), 5)) g.DrawPath(p, a);
                using (var b = Rounded(new RectangleF(-5, -2, 10, 13), 5)) g.DrawPath(p, b);
                break;
            case "image": g.DrawRectangle(p, 2, 3, 20, 17); g.DrawEllipse(p, 6, 6, 3, 3); g.DrawLines(p, new PointF[] {new(3, 18), new(10, 12), new(14, 16), new(18, 11), new(22, 16) }); break;
            case "document": g.DrawLines(p, new PointF[] {new(6, 2), new(15, 2), new(20, 7), new(20, 22), new(6, 22), new(6, 2) }); g.DrawLines(p, new PointF[] {new(15, 2), new(15, 7), new(20, 7) }); g.DrawLine(p, 9, 12, 16, 12); g.DrawLine(p, 9, 16, 16, 16); break;
            case "audio": g.DrawLines(p, new PointF[] {new(8, 18), new(8, 5), new(20, 2), new(20, 15) }); g.DrawLine(p, 8, 8, 20, 5); g.FillEllipse(brush, 2, 16, 7, 5); g.FillEllipse(brush, 14, 13, 7, 5); break;
            case "pin": g.DrawLines(p, new PointF[] {new(7, 3), new(17, 3), new(15, 6), new(15, 12), new(19, 16), new(5, 16), new(9, 12), new(9, 6), new(7, 3) }); g.DrawLine(p, 12, 16, 12, 23); break;
            case "pause": g.FillRectangle(brush, 6, 4, 4, 16); g.FillRectangle(brush, 14, 4, 4, 16); break;
            case "play": g.FillPolygon(brush, new Point[] { new(7, 3), new(21, 12), new(7, 21) }); break;
            case "expand": g.DrawLines(p, new PointF[] {new(3, 9), new(3, 3), new(9, 3) }); g.DrawLine(p, 3, 3, 10, 10); g.DrawLines(p, new PointF[] {new(15, 21), new(21, 21), new(21, 15) }); g.DrawLine(p, 14, 14, 21, 21); break;
            case "gear": g.DrawEllipse(p, 5, 5, 14, 14); g.DrawEllipse(p, 9, 9, 6, 6); for (int i = 0; i < 8; i++) { double a = i * Math.PI / 4; g.DrawLine(p, 12 + (float)Math.Cos(a) * 8, 12 + (float)Math.Sin(a) * 8, 12 + (float)Math.Cos(a) * 11, 12 + (float)Math.Sin(a) * 11); } break;
        }
        g.Restore(state);
    }
}

internal sealed class IconButton : Button
{
    private bool active;
    public string IconName { get; set; }
    public bool Active { get => active; set { active = value; Invalidate(); } }
    public IconButton(string icon) { IconName = icon; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); Cursor = Cursors.Hand; FlatStyle = FlatStyle.Flat; }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? Theme.Panel); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (Active || ClientRectangle.Contains(PointToClient(Cursor.Position))) { using var b = new SolidBrush(Color.FromArgb(239, 247, 255)); using var p = Theme.Rounded(new RectangleF(1, 1, Width - 2, Height - 2), 8 * DeviceDpi / 96f); e.Graphics.FillPath(b, p); }
        float size = 21 * DeviceDpi / 96f;
        Theme.Icon(e.Graphics, IconName, new RectangleF((Width - size) / 2, (Height - size) / 2, size, size), Active ? Theme.Blue : Theme.Muted);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -3, -3));
    }
}

internal sealed class HistoryCard : Control
{
    public ClipItem Item { get; }
    public event Action? Restore, Delete, Pin;
    private readonly bool large;
    private bool selected, dragging;
    private Point down;
    private Image? thumbnail;
    private Size originalSize;
    private readonly ContextMenuStrip menu = new();
    public bool Selected { get => selected; set { selected = value; Invalidate(); } }
    private float S => DeviceDpi / 96f;
    public HistoryCard(ClipItem item, bool largePreview)
    {
        Item = item; large = largePreview && item.Kind is "image" or "document" or "audio";
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent; Cursor = Cursors.Hand; TabStop = false;
        AccessibleName = item.FileName ?? item.Text; AccessibleRole = AccessibleRole.ListItem;
        Height = (int)((large ? 236 : 120) * S);
        if (item.Payload != null && (item.Kind == "image" || new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" }.Contains(Path.GetExtension(item.FileName ?? "").ToLowerInvariant())))
        {
            try { using var img = Image.FromFile(item.Payload); originalSize = img.Size; thumbnail = new Bitmap(img, Fit(img.Size, new Size(600, 400)).Size); }
            catch (Exception e) when (e is IOException or ArgumentException or OutOfMemoryException) { }
        }
        menu.Items.Add("Copy to clipboard", null, (_, _) => Restore?.Invoke());
        menu.Items.Add(item.Pinned ? "Unpin item" : "Pin item", null, (_, _) => Pin?.Invoke());
        if (item.Kind == "link") menu.Items.Add("Open link", null, (_, _) => { if (Uri.TryCreate(item.Text, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") Process.Start(new ProcessStartInfo(item.Text) { UseShellExecute = true }); });
        menu.Items.Add("Delete", null, (_, _) => Delete?.Invoke()); ContextMenuStrip = menu;
    }
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e); Height = (int)((large ? 236 : 120) * S);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        using var shape = Theme.Rounded(new RectangleF(1, 1, Width - 3, Height - 3), 13 * S);
        using var white = new SolidBrush(Color.FromArgb(249, 252, 255)); g.FillPath(white, shape);
        if (Selected) { using var outline = new Pen(Theme.Blue, 2 * S); g.DrawPath(outline, shape); }
        using var red = new SolidBrush(Color.FromArgb(255, 64, 72)); g.FillEllipse(red, 14 * S, 14 * S, 10 * S, 10 * S);
        using var cross = new Pen(Color.White, S); g.DrawLine(cross, 17 * S, 17 * S, 21 * S, 21 * S); g.DrawLine(cross, 21 * S, 17 * S, 17 * S, 21 * S);
        DrawText(g, Item.CopiedAt.ToLocalTime().ToString("t"), new RectangleF(Width - 108 * S, 12 * S, 91 * S, 20 * S), 10, Theme.Muted, false, StringAlignment.Far);
        if (Item.Pinned) Theme.Icon(g, "pin", new RectangleF(31 * S, 12 * S, 13 * S, 13 * S), Theme.Blue);
        float left = 70 * S;
        if (large)
        {
            var preview = new Rectangle((int)(15 * S), (int)(38 * S), (int)(Width - 30 * S), (int)(145 * S));
            if (thumbnail != null) DrawImage(g, thumbnail, preview);
            else Theme.Icon(g, Item.Kind, new RectangleF((Width - 60 * S) / 2, 75 * S, 60 * S, 60 * S), Theme.Muted);
            DrawText(g, Item.FileName ?? "Image", new RectangleF(15 * S, 190 * S, Width - 30 * S, 21 * S), 11, Theme.Ink, true);
            DrawText(g, Metadata(), new RectangleF(15 * S, 213 * S, Width - 30 * S, 19 * S), 10, Theme.Muted);
        }
        else
        {
            if (thumbnail != null) { DrawImage(g, thumbnail, new Rectangle((int)(14 * S), (int)(39 * S), (int)(80 * S), (int)(63 * S))); left = 105 * S; }
            else
            {
                using var tile = Theme.Rounded(new RectangleF(14 * S, 39 * S, 44 * S, 44 * S), 10 * S); g.FillPath(Brushes.White, tile);
                Theme.Icon(g, Item.Kind, new RectangleF(23 * S, 48 * S, 26 * S, 26 * S), Item.Kind == "link" ? Color.MediumPurple : Theme.Blue);
            }
            DrawText(g, (Item.FileName ?? Item.Text).Replace('\r', ' ').Replace('\n', ' '), new RectangleF(left, 39 * S, Width - left - 13 * S, 24 * S), 12, Theme.Ink, true);
            DrawText(g, Metadata(), new RectangleF(left, 65 * S, Width - left - 13 * S, 20 * S), 10.5f, Theme.Muted);
            DrawText(g, Item.Source ?? "Clipboard", new RectangleF(left, 86 * S, Width - left - 13 * S, 19 * S), 10, Theme.Muted);
        }
    }
    private string Metadata()
    {
        if (Item.Kind == "text") return $"Text · {Item.Text.Length:N0} characters";
        if (Item.Kind == "link") return Uri.TryCreate(Item.Text, UriKind.Absolute, out var uri) ? uri.Host : "Link";
        var ext = Path.GetExtension(Item.FileName ?? "").TrimStart('.').ToUpperInvariant();
        return thumbnail != null ? $"{originalSize.Width} × {originalSize.Height} · {ext}" : (ext.Length > 0 ? ext + " file" : Item.Kind);
    }
    private static void DrawText(Graphics g, string text, RectangleF rect, float size, Color color, bool bold = false, StringAlignment align = StringAlignment.Near)
    {
        using var font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
        using var brush = new SolidBrush(color); using var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, Alignment = align };
        g.DrawString(text, font, brush, rect, format);
    }
    private static Rectangle Fit(Size image, Size available) { var ratio = Math.Min((double)available.Width / image.Width, (double)available.Height / image.Height); var w = Math.Max(1, (int)(image.Width * ratio)); var h = Math.Max(1, (int)(image.Height * ratio)); return new Rectangle((available.Width - w) / 2, (available.Height - h) / 2, w, h); }
    private static void DrawImage(Graphics g, Image image, Rectangle rect) { var target = Fit(image.Size, rect.Size); target.Offset(rect.Location); g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(image, target); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); down = e.Location; dragging = false; }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.Button != MouseButtons.Left || dragging || (Math.Abs(e.X - down.X) < SystemInformation.DragSize.Width && Math.Abs(e.Y - down.Y) < SystemInformation.DragSize.Height)) return;
        dragging = true;
        try
        {
            var data = new DataObject();
            if (Item.Payload != null)
            {
                var folder = Path.Combine(Path.GetTempPath(), "ClipHat", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, Path.GetFileName(Item.FileName ?? "Image.png")); File.Copy(Item.Payload, path);
                data.SetData(DataFormats.FileDrop, new[] { path });
            }
            else data.SetText(Item.Text);
            DoDragDrop(data, DragDropEffects.Copy);
        }
        catch (IOException e2) { MessageBox.Show(e2.Message, "ClipHat"); }
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e); if (e.Button != MouseButtons.Left || dragging) return;
        if (new RectangleF(7 * S, 7 * S, 26 * S, 26 * S).Contains(e.Location)) Delete?.Invoke(); else Restore?.Invoke();
    }
    protected override void Dispose(bool disposing) { if (disposing) { thumbnail?.Dispose(); menu.Dispose(); } base.Dispose(disposing); }
}
