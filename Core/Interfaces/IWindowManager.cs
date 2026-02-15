namespace Winharpoon.Core.Interfaces;

public interface IWindowManager
{
    IntPtr GetForegroundWindow();
    bool SwitchToWindow(IntPtr hwnd);
    string GetWindowTitle(IntPtr hwnd);
    string? GetProcessName(IntPtr hwnd);
    bool IsValidWindow(IntPtr hwnd);
    IntPtr? FindWindowByProcessAndTitle(string processName, string titlePattern);
}
