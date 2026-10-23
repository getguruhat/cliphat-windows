using System.Globalization;

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
            CheckStrings();
            // German has the longest text and Japanese needs font fallback; save both for a look.
            foreach (var culture in new[] { "de-DE", "ja-JP" })
            {
                Strings.Use(new CultureInfo(culture));
                using (var localized = new MainWindow(history, _ => { }, () => { }, () => { }))
                {
                    localized.OpenPanel(screen, "Left"); Pump(400);
                    Capture(localized, $"screenshots/panel-{culture}.png");
                }
                using var settings = new SettingsWindow(history);
                settings.Show(); Pump(150);
                foreach (var page in new[] { "General", "History", "Privacy", "About" })
                {
                    settings.SelectPage(page); Pump(100); Capture(settings, $"screenshots/settings-{page.ToLowerInvariant()}-{culture}.png");
                }
                settings.Close();
            }
            Strings.Use(new CultureInfo("en-US"));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    /// Every translation is picked by its culture, has no empty text, and its templates format.
    private static void CheckStrings()
    {
        foreach (var name in new[] { "es-ES", "de-DE", "fr-FR", "it-IT", "pt-BR", "ja-JP", "zh-CN", "ko-KR" })
        {
            var strings = Strings.For(new CultureInfo(name));
            var values = typeof(Strings).GetProperties().Where(p => p.PropertyType == typeof(string)).Select(p => (string)p.GetValue(strings)!).ToList();
            foreach (var value in values) _ = string.Format(value, "x", "y");
            _ = strings.DayName(DateTime.Today);
            if (strings.OpenHistory == "Open history" || values.Any(v => v.Length == 0))
                throw new InvalidOperationException($"The {name} translation is missing.");
        }
        if (Strings.For(new CultureInfo("zh-TW")).OpenHistory != "Open history")
            throw new InvalidOperationException("Traditional Chinese did not fall back to English.");
        if (Strings.For(new CultureInfo("de-AT")).HistoryShortcut != "Strg+Umschalt+V")
            throw new InvalidOperationException("Regional German did not use German.");
    }
    private static void Capture(Form form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }
    private static void Pump(int milliseconds)
    {
        var end = Compat.TickCount64 + milliseconds;
        while (Compat.TickCount64 < end) { Application.DoEvents(); Thread.Sleep(10); }
    }
}
