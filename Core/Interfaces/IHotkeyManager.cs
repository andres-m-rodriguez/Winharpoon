namespace Winharpoon.Core.Interfaces;

public interface IHotkeyManager : IDisposable
{
    event Action? OnMarkModeActivated;
    event Action<int>? OnSlotActivated;
    event Action? OnCycleNext;
    event Action? OnCyclePrev;
    event Action? OnShowOverlay;
    event Action<int>? OnClearSlot;
    event Action? OnClearAll;

    bool RegisterHotkeys();
    void ProcessHotkey(int hotkeyId);
}
