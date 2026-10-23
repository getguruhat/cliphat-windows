// The usings the .NET 8 Windows Forms SDK added on its own.
global using System;
global using System.Collections.Generic;
global using System.Drawing;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Windows.Forms;

using System.Diagnostics;
using System.Security.Cryptography;

namespace ClipHat;

/// Stand-ins for the .NET 8 APIs that .NET Framework 4.8 doesn't have.
internal static class Compat
{
    private static readonly Stopwatch clock = Stopwatch.StartNew();

    /// Milliseconds on a steady clock, for Environment.TickCount64.
    public static long TickCount64 => clock.ElapsedMilliseconds;

    public static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);
    public static double Clamp(double value, double min, double max) => Math.Min(Math.Max(value, min), max);

    /// Uppercase hex, like Convert.ToHexString, so fingerprints from 1.3 still match.
    public static string Sha256Hex(byte[] data)
    {
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
    }

    /// string.Contains(string, StringComparison).
    public static bool Contains(this string text, string value, StringComparison comparison) => text.IndexOf(value, comparison) >= 0;
}
