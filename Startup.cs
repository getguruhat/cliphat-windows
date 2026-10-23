using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.ApplicationModel;
using Windows.Storage;

namespace ClipHat;

/// "Launch ClipHat at login", and the other things that work differently when ClipHat
/// runs from its Microsoft Store (MSIX) package.
internal static class Startup
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClipHat";
    /// Matches the StartupTask TaskId in msix/AppxManifest.xml.
    private const string TaskId = "ClipHatStartup";
    private const int NoPackage = 15700; // APPMODEL_ERROR_NO_PACKAGE

    [DllImport("kernel32.dll")]
    private static extern int GetCurrentPackageFullName(ref uint length, IntPtr name);

    public static readonly bool Packaged = IsPackaged();

    private static bool IsPackaged()
    {
        uint length = 0;
        return GetCurrentPackageFullName(ref length, IntPtr.Zero) != NoPackage;
    }

    public static bool Enabled
    {
        get
        {
            if (Packaged) return GetTask().State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
        set
        {
            if (Packaged)
            {
                var task = GetTask();
                if (!value) { task.Disable(); return; }
                var state = Task.Run(async () => await task.RequestEnableAsync()).GetAwaiter().GetResult();
                // Once the user turns ClipHat off in Windows' own startup settings, only they can turn it back on.
                if (state == StartupTaskState.DisabledByUser)
                    MessageBox.Show(Strings.Current.StartupBlocked, "ClipHat");
                return;
            }
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value) key.SetValue(ValueName, '"' + Application.ExecutablePath + '"'); else key.DeleteValue(ValueName, false);
        }
    }

    private static StartupTask GetTask() =>
        Task.Run(async () => await StartupTask.GetAsync(TaskId)).GetAwaiter().GetResult();

    /// Where copies of saved files go when they're put back on the clipboard or dragged into
    /// another app. A packaged app's writes to %TEMP% are redirected into its package, where
    /// other apps can't find them, so the Store version uses the package's own temp folder.
    public static string ExportFolder => Path.Combine(Packaged ? ApplicationData.Current.TemporaryFolder.Path : Path.GetTempPath(), "ClipHat");
}
