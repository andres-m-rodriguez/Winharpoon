using Microsoft.Win32;

namespace Winharpoon;

public static class StartupManager
{
    private const string AppName = "Winharpoon";
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
        return key?.GetValue(AppName) != null;
    }

    public static void SetStartupEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
        if (key == null) return;

        if (enabled)
        {
            string exePath = Environment.ProcessPath ?? "";
            if (!string.IsNullOrEmpty(exePath))
            {
                key.SetValue(AppName, $"\"{exePath}\"");
            }
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }

    public static void ToggleStartup()
    {
        SetStartupEnabled(!IsStartupEnabled());
    }
}
