namespace ClipHat;

internal static class PanelSmokeTest
{
    public static void Run()
    {
        var folder = Path.Combine(Path.GetTempPath(), "ClipHat-check-" + Guid.NewGuid().ToString("N"));
        try
        {
            var history = new History(folder);
            history.Add(new ClipItem { Kind = "image", Text = "Image", FileName = "ClipHat.png", Payload = history.SavePayload(File.ReadAllBytes("assets/ClipHat.png"), "ClipHat.png"), Source = "Photos", Fingerprint = "image" });
            history.Add(new ClipItem { Kind = "document", Text = "C:\\Documents\\Project notes.pdf", FileName = "Project notes.pdf", Source = "Explorer", Fingerprint = "document" });
            history.Add(new ClipItem { Kind = "link", Text = "https://github.com/getguruhat/cliphat-windows", Source = "Microsoft Edge", Fingerprint = "link" });
            history.Add(new ClipItem { Kind = "text", Text = "Your clipboard, always at hand.", Source = "Notepad", Fingerprint = "text", Pinned = true });
            using var panel = new MainWindow(history, _ => { }, () => { }, () => { });
            var screen = Screen.PrimaryScreen!;
            foreach (var side in new[] { "Left", "Right" })
            {
                panel.OpenPanel(screen, side); Pump(450);
                var area = screen.WorkingArea;
                if (!panel.Visible || panel.FormBorderStyle != FormBorderStyle.None || panel.Top != area.Top || panel.Height != area.Height)
                    throw new InvalidOperationException("Side panel did not open borderless at full height.");
                if (side == "Left" ? panel.Left != area.Left : panel.Right != area.Right)
                    throw new InvalidOperationException("Side panel did not finish at the selected screen edge.");
                panel.ClosePanel(); Pump(300);
                if (panel.Visible) throw new InvalidOperationException("Side panel did not dismiss.");
            }
            panel.TogglePanelPin(); panel.OpenPanel(screen, "Left"); Pump(400);
            using var other = new Form { Text = "Other app", TopMost = true, Bounds = new Rectangle(500, 150, 250, 200) };
            other.Show(); other.Activate(); Pump(400);
            if (!panel.Visible || !panel.PanelPinned || !new History(folder).Settings.PanelPinned)
                throw new InvalidOperationException("Thumbtack did not keep the panel open or persist.");
            other.Hide(); panel.Activate(); Pump(100);
            Directory.CreateDirectory("screenshots");
            Capture(panel, "screenshots/compact.png");
            history.Settings.LargePreviews = true; panel.RefreshItems(); Pump(100);
            Capture(panel, "screenshots/large-previews.png");
            using (var settings = new SettingsWindow(history))
            {
                settings.Show(); Pump(150);
                foreach (var page in new[] { "General", "History", "Privacy", "About" })
                {
                    settings.SelectPage(page); Pump(100); Capture(settings, "screenshots/settings-" + page.ToLowerInvariant() + ".png");
                }
                settings.Close();
            }
            panel.Activate(); Pump(100);
            panel.TogglePanelPin(); other.Show(); other.Activate(); Pump(400);
            if (panel.Visible) throw new InvalidOperationException("Unpinned panel did not dismiss when another app was activated.");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    private static void Capture(Form form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }
    private static void Pump(int milliseconds)
    {
        var end = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < end) { Application.DoEvents(); Thread.Sleep(10); }
    }
}
