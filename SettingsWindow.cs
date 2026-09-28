using Microsoft.Win32;

namespace ClipHat;

internal sealed class SettingsWindow : Form
{
    public SettingsWindow(History history)
    {
        Text = "ClipHat Settings — 1.2.0"; Size = new Size(440, 540); StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
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
        AddToggle("Keep panel open (thumbtack)", settings.PanelPinned, x => settings.PanelPinned = x);
        AddToggle("Large image and file previews", settings.LargePreviews, x => settings.LargePreviews = x);
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
