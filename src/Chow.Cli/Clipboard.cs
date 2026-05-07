using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Chow.Cli;

internal static class Clipboard
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    /// <summary>
    /// True when clipboard operations are available on the current platform
    /// (Windows or macOS).
    /// </summary>
    public static bool IsSupported { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Copies <paramref name="text"/> to the system clipboard. Returns false on
    /// unsupported platforms or if the underlying clipboard call failed.
    /// </summary>
    public static bool TrySetText(string text)
    {
        if (text == null)
        {
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return TrySetTextWindows(text);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return TrySetTextMac(text);
        }

        return false;
    }

    /// <summary>
    /// Reads text from the system clipboard into <paramref name="text"/>.
    /// Returns false on unsupported platforms or when no text is available.
    /// </summary>
    public static bool TryGetText(out string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return TryGetTextWindows(out text);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return TryGetTextMac(out text);
        }

        text = string.Empty;
        return false;
    }

    #region Windows Methods

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr GlobalSize(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    private static bool TrySetTextWindows(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            return false;
        }

        IntPtr hGlobal = IntPtr.Zero;

        try
        {
            EmptyClipboard();

            int byteCount = (text.Length + 1) * sizeof(char);
            hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)byteCount);

            if (hGlobal == IntPtr.Zero)
            {
                return false;
            }

            IntPtr target = GlobalLock(hGlobal);

            if (target == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                Marshal.WriteInt16(target, text.Length * sizeof(char), 0);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
            {
                return false;
            }

            hGlobal = IntPtr.Zero;
            return true;
        }
        finally
        {
            if (hGlobal != IntPtr.Zero)
            {
                GlobalFree(hGlobal);
            }

            CloseClipboard();
        }
    }

    private static bool TryGetTextWindows(out string text)
    {
        text = string.Empty;

        if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
        {
            return false;
        }

        if (!OpenClipboard(IntPtr.Zero))
        {
            return false;
        }

        try
        {
            IntPtr handle = GetClipboardData(CF_UNICODETEXT);

            if (handle == IntPtr.Zero)
            {
                return false;
            }

            IntPtr ptr = GlobalLock(handle);

            if (ptr == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                int byteSize = (int)GlobalSize(handle);

                if (byteSize <= 0)
                {
                    text = string.Empty;
                    return true;
                }

                int maxChars = byteSize / sizeof(char);
                short[] shorts = new short[maxChars];
                Marshal.Copy(ptr, shorts, 0, maxChars);

                char[] buffer = new char[maxChars];
                int length = 0;

                for (length = 0; length < maxChars; length++)
                {
                    if (shorts[length] == 0)
                    {
                        break;
                    }

                    buffer[length] = (char)shorts[length];
                }

                text = new string(buffer, 0, length);
                return true;
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    #endregion

    #region macOS Methods

    private static bool TrySetTextMac(string text)
    {
        try
        {
            ProcessStartInfo info = new ProcessStartInfo("pbcopy")
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process? process = Process.Start(info);

            if (process == null)
            {
                return false;
            }

            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetTextMac(out string text)
    {
        text = string.Empty;

        try
        {
            ProcessStartInfo info = new ProcessStartInfo("pbpaste")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process? process = Process.Start(info);

            if (process == null)
            {
                return false;
            }

            text = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
