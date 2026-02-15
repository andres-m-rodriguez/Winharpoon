using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Winharpoon;

public static class WindowManager
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;
    private const int GWL_STYLE = -16;
    private const long WS_VISIBLE = 0x10000000L;
    private const int ASFW_ANY = -1;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static IntPtr GetCurrentForegroundWindow()
    {
        return GetForegroundWindow();
    }

    public static string GetWindowTitle(IntPtr hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        if (length == 0) return string.Empty;

        var sb = new StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public static string? GetProcessName(IntPtr hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint processId);
        if (processId == 0) return null;

        try
        {
            var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsValidWindow(IntPtr hWnd)
    {
        return hWnd != IntPtr.Zero && IsWindow(hWnd) && IsWindowVisible(hWnd);
    }

    public static bool SwitchToWindow(IntPtr hWnd)
    {
        if (!IsValidWindow(hWnd))
            return false;

        // Restore if minimized
        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, SW_RESTORE);
        }

        // Try multiple methods to bring window to foreground
        AllowSetForegroundWindow(ASFW_ANY);

        // Attach to the foreground thread to get permission
        IntPtr foregroundHwnd = GetForegroundWindow();
        uint foregroundThread = GetWindowThreadProcessId(foregroundHwnd, out _);
        uint currentThread = GetCurrentThreadId();

        if (foregroundThread != currentThread)
        {
            AttachThreadInput(currentThread, foregroundThread, true);
            bool result = SetForegroundWindow(hWnd);
            AttachThreadInput(currentThread, foregroundThread, false);
            return result;
        }

        return SetForegroundWindow(hWnd);
    }

    public static IntPtr? FindWindowByProcessAndTitle(string processName, string titlePattern)
    {
        IntPtr? foundWindow = null;
        IntPtr shellWindow = GetShellWindow();

        EnumWindows((hWnd, lParam) =>
        {
            // Skip shell window and invisible windows
            if (hWnd == shellWindow || !IsWindowVisible(hWnd))
                return true;

            // Check if style includes WS_VISIBLE
            long style = GetWindowLongPtr(hWnd, GWL_STYLE).ToInt64();
            if ((style & WS_VISIBLE) == 0)
                return true;

            // Get process name
            string? procName = GetProcessName(hWnd);
            if (procName == null || !procName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Get window title
            string title = GetWindowTitle(hWnd);
            if (string.IsNullOrEmpty(title))
                return true;

            // Check if title contains the pattern
            if (title.Contains(titlePattern, StringComparison.OrdinalIgnoreCase))
            {
                foundWindow = hWnd;
                return false; // Stop enumeration
            }

            return true;
        }, IntPtr.Zero);

        return foundWindow;
    }

    public static List<(IntPtr Handle, string ProcessName, string Title)> GetAllWindows()
    {
        var windows = new List<(IntPtr, string, string)>();
        IntPtr shellWindow = GetShellWindow();

        EnumWindows((hWnd, lParam) =>
        {
            if (hWnd == shellWindow || !IsWindowVisible(hWnd))
                return true;

            string? procName = GetProcessName(hWnd);
            if (procName == null)
                return true;

            string title = GetWindowTitle(hWnd);
            if (string.IsNullOrEmpty(title))
                return true;

            windows.Add((hWnd, procName, title));
            return true;
        }, IntPtr.Zero);

        return windows;
    }
}
