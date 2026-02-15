namespace Winharpoon.Core.Interfaces;

public interface ITrayIcon : IDisposable
{
    event Action? OnExitRequested;

    void ShowNotification(string title, string message, int durationMs = 2000);
    void ShowMarkModeNotification();
    void ShowMarkedNotification(int slot, string processName);
    void ShowSwitchFailedNotification(int slot);
}
