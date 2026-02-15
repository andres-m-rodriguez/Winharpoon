using Winharpoon.Core.Interfaces;

namespace Winharpoon.Platforms.Linux;

// Stub implementation for future Linux support
public class LinuxWindowManager : IWindowManager
{
    public IntPtr GetForegroundWindow()
    {
        throw new PlatformNotSupportedException("Linux support not yet implemented");
    }

    public bool SwitchToWindow(IntPtr hwnd)
    {
        throw new PlatformNotSupportedException("Linux support not yet implemented");
    }

    public string GetWindowTitle(IntPtr hwnd)
    {
        throw new PlatformNotSupportedException("Linux support not yet implemented");
    }

    public string? GetProcessName(IntPtr hwnd)
    {
        throw new PlatformNotSupportedException("Linux support not yet implemented");
    }

    public bool IsValidWindow(IntPtr hwnd)
    {
        throw new PlatformNotSupportedException("Linux support not yet implemented");
    }

    public IntPtr? FindWindowByProcessAndTitle(string processName, string titlePattern)
    {
        throw new PlatformNotSupportedException("Linux support not yet implemented");
    }
}
