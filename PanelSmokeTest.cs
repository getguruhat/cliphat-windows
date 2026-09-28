namespace ClipHat;

internal static class PanelSmokeTest
{
    public static void Run()
    {
        using var panel = new MainWindow(new History(), _ => { }, () => { }, () => { });
        var screen = Screen.PrimaryScreen!;
        foreach (var side in new[] { "Left", "Right" })
        {
            panel.OpenPanel(screen, side);
            Pump(350);
            var area = screen.WorkingArea;
            if (!panel.Visible || panel.FormBorderStyle != FormBorderStyle.None || panel.Top != area.Top || panel.Height != area.Height)
                throw new InvalidOperationException("Side panel did not open borderless at full height.");
            if (side == "Left" ? panel.Left != area.Left : panel.Right != area.Right)
                throw new InvalidOperationException("Side panel did not finish at the selected screen edge.");
            panel.ClosePanel();
            Pump(300);
            if (panel.Visible) throw new InvalidOperationException("Side panel did not dismiss.");
        }
    }
    private static void Pump(int milliseconds)
    {
        var end = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < end) { Application.DoEvents(); Thread.Sleep(10); }
    }
}
