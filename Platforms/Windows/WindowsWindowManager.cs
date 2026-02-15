using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Winharpoon.Core.Interfaces;

namespace Winharpoon.Platforms.Windows;

public class WindowsWindowManager : IWindowManager
{
    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    private static extern IntPtr NativeGetForegroundWindow();

    [DllImport("user32.dll")]
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
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
    private static extern uint GetWindowThreadProcessIdNoOut(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int SW_RESTORE = 9;

    public IntPtr GetForegroundWindow()
    {
        return NativeGetForegroundWindow();
    }

    public bool SwitchToWindow(IntPtr hwnd)
    {
        if (!IsWindow(hwnd))
            return false;

        // If window is minimized, restore it
        if (IsIconic(hwnd))
        {
            ShowWindow(hwnd, SW_RESTORE);
        }

        // Try to set foreground window with thread input attachment trick
        uint foregroundThread = GetWindowThreadProcessIdNoOut(NativeGetForegroundWindow(), IntPtr.Zero);
        uint currentThread = GetCurrentThreadId();

        if (foregroundThread != currentThread)
        {
            AttachThreadInput(currentThread, foregroundThread, true);
            SetForegroundWindow(hwnd);
            AttachThreadInput(currentThread, foregroundThread, false);
        }
        else
        {
            SetForegroundWindow(hwnd);
        }

        // Fallback
        SwitchToThisWindow(hwnd, true);

        return true;
    }

    public string GetWindowTitle(IntPtr hwnd)
    {
        int length = GetWindowTextLength(hwnd);
        if (length == 0)
            return string.Empty;

        var sb = new StringBuilder(length + 1);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public string? GetProcessName(IntPtr hwnd)
    {
        GetWindowThreadProcessId(hwnd, out uint processId);
        if (processId == 0)
            return null;

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

    public bool IsValidWindow(IntPtr hwnd)
    {
        return hwnd != IntPtr.Zero && IsWindow(hwnd) && IsWindowVisible(hwnd);
    }

    public IntPtr? FindWindowByProcessAndTitle(string processName, string titlePattern)
    {
        IntPtr? foundWindow = null;
        var shellWindow = GetShellWindow();

        EnumWindows((hWnd, lParam) =>
        {
            if (hWnd == shellWindow)
                return true;

            if (!IsWindowVisible(hWnd))
                return true;

            string? currentProcessName = GetProcessName(hWnd);
            if (currentProcessName == null || !currentProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                return true;

            string title = GetWindowTitle(hWnd);
            if (string.IsNullOrEmpty(title))
                return true;

            if (title.Contains(titlePattern, StringComparison.OrdinalIgnoreCase))
            {
                foundWindow = hWnd;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return foundWindow;
    }
}
