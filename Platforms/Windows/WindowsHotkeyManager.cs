using System.Runtime.InteropServices;
using Winharpoon.Core.Interfaces;

namespace Winharpoon.Platforms.Windows;

public class WindowsHotkeyManager : IHotkeyManager
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_NOREPEAT = 0x4000;

    private const uint VK_0 = 0x30;
    private const uint VK_1 = 0x31;
    private const uint VK_H = 0x48;
    private const uint VK_M = 0x4D;
    private const uint VK_N = 0x4E;
    private const uint VK_P = 0x50;

    public const int HOTKEY_MARK_MODE = 100;
    public const int HOTKEY_SLOT_BASE = 200;
    public const int HOTKEY_CYCLE_NEXT = 300;
    public const int HOTKEY_CYCLE_PREV = 301;
    public const int HOTKEY_SHOW_OVERLAY = 302;
    public const int HOTKEY_CLEAR_SLOT_BASE = 400;
    public const int HOTKEY_CLEAR_ALL = 409;

    private readonly IntPtr _hwnd;
    private readonly List<int> _registeredHotkeys = new();
    private bool _disposed;

    public event Action? OnMarkModeActivated;
    public event Action<int>? OnSlotActivated;
    public event Action? OnCycleNext;
    public event Action? OnCyclePrev;
    public event Action? OnShowOverlay;
    public event Action<int>? OnClearSlot;
    public event Action? OnClearAll;

    public WindowsHotkeyManager(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public bool RegisterHotkeys()
    {
        bool success = true;

        success &= TryRegister(HOTKEY_MARK_MODE, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_M);

        for (int i = 0; i < 9; i++)
        {
            int hotkeyId = HOTKEY_SLOT_BASE + i;
            uint vk = VK_1 + (uint)i;
            success &= TryRegister(hotkeyId, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, vk);
        }

        success &= TryRegister(HOTKEY_CYCLE_NEXT, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_N);
        success &= TryRegister(HOTKEY_CYCLE_PREV, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_P);
        success &= TryRegister(HOTKEY_SHOW_OVERLAY, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_H);
        success &= TryRegister(HOTKEY_CLEAR_ALL, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_0);

        return success;
    }

    private bool TryRegister(int id, uint modifiers, uint vk)
    {
        if (RegisterHotKey(_hwnd, id, modifiers, vk))
        {
            _registeredHotkeys.Add(id);
            return true;
        }
        return false;
    }

    public void ProcessHotkey(int hotkeyId)
    {
        if (hotkeyId == HOTKEY_MARK_MODE)
        {
            OnMarkModeActivated?.Invoke();
        }
        else if (hotkeyId >= HOTKEY_SLOT_BASE && hotkeyId < HOTKEY_SLOT_BASE + 9)
        {
            int slot = hotkeyId - HOTKEY_SLOT_BASE + 1;
            OnSlotActivated?.Invoke(slot);
        }
        else if (hotkeyId == HOTKEY_CYCLE_NEXT)
        {
            OnCycleNext?.Invoke();
        }
        else if (hotkeyId == HOTKEY_CYCLE_PREV)
        {
            OnCyclePrev?.Invoke();
        }
        else if (hotkeyId == HOTKEY_SHOW_OVERLAY)
        {
            OnShowOverlay?.Invoke();
        }
        else if (hotkeyId == HOTKEY_CLEAR_ALL)
        {
            OnClearAll?.Invoke();
        }
        else if (hotkeyId >= HOTKEY_CLEAR_SLOT_BASE && hotkeyId < HOTKEY_CLEAR_SLOT_BASE + 9)
        {
            int slot = hotkeyId - HOTKEY_CLEAR_SLOT_BASE + 1;
            OnClearSlot?.Invoke(slot);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (int hotkeyId in _registeredHotkeys)
        {
            UnregisterHotKey(_hwnd, hotkeyId);
        }

        _registeredHotkeys.Clear();
        _disposed = true;
    }
}
