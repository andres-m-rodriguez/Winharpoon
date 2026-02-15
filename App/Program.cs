using Winharpoon.Core.Interfaces;
using Winharpoon.Core.Services;
using Winharpoon.Platforms.Windows;

namespace Winharpoon.App;

internal class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var mutex = new Mutex(true, "Winharpoon_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Winharpoon is already running.", "Winharpoon", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Create platform-specific implementations
        IWindowManager windowManager = new WindowsWindowManager();
        IStartupManager startupManager = new WindowsStartupManager();

        using var app = new WinharpoonApp(windowManager, startupManager);
        app.Run();
    }
}

internal class WinharpoonApp : IDisposable
{
    private readonly HarpoonState _state;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly TrayIcon _trayIcon;
    private readonly HotkeyWindow _hotkeyWindow;
    private readonly MarksOverlay _marksOverlay;
    private readonly System.Windows.Forms.Timer _markModeTimer;

    private bool _inMarkMode;
    private DateTime _markModeStartTime;
    private static readonly TimeSpan MarkModeTimeout = TimeSpan.FromSeconds(3);
    private bool _disposed;

    public WinharpoonApp(IWindowManager windowManager, IStartupManager startupManager)
    {
        _state = new HarpoonState(windowManager);
        _hotkeyWindow = new HotkeyWindow();
        _hotkeyManager = new WindowsHotkeyManager(_hotkeyWindow.Handle);
        _trayIcon = new TrayIcon(_state, startupManager);
        _marksOverlay = new MarksOverlay(_state);

        _markModeTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _markModeTimer.Tick += CheckMarkModeTimeout;

        _hotkeyWindow.OnHotkey += OnHotkey;
        _hotkeyManager.OnMarkModeActivated += OnMarkModeActivated;
        _hotkeyManager.OnSlotActivated += OnSlotActivated;
        _hotkeyManager.OnCycleNext += OnCycleNext;
        _hotkeyManager.OnCyclePrev += OnCyclePrev;
        _hotkeyManager.OnShowOverlay += OnShowOverlay;
        _hotkeyManager.OnClearAll += OnClearAll;
        _hotkeyManager.OnClearSlot += OnClearSlot;
        _trayIcon.OnExitRequested += () => Application.Exit();

        if (!_hotkeyManager.RegisterHotkeys())
        {
            MessageBox.Show("Failed to register some hotkeys. Some key combinations may already be in use.",
                "Winharpoon Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public void Run()
    {
        _trayIcon.ShowNotification("Winharpoon", "Running. Ctrl+Shift+M to mark, Ctrl+Shift+H for overlay.", 2000);
        Application.Run();
    }

    private void OnHotkey(int hotkeyId)
    {
        _hotkeyManager.ProcessHotkey(hotkeyId);
    }

    private void OnMarkModeActivated()
    {
        _inMarkMode = true;
        _markModeStartTime = DateTime.UtcNow;
        _markModeTimer.Start();
        _trayIcon.ShowMarkModeNotification();
    }

    private void OnSlotActivated(int slot)
    {
        if (_inMarkMode)
        {
            _state.MarkCurrentWindow(slot);

            var mark = _state.GetMark(slot);
            if (mark != null)
            {
                _trayIcon.ShowMarkedNotification(slot, mark.ProcessName);
            }

            ExitMarkMode();
        }
        else
        {
            bool success = _state.SwitchToSlot(slot);
            if (!success)
            {
                _trayIcon.ShowSwitchFailedNotification(slot);
            }
        }
    }

    private void OnCycleNext()
    {
        if (_state.GetMarkCount() == 0)
        {
            _trayIcon.ShowNotification("Winharpoon", "No marks to cycle through.", 1500);
            return;
        }
        _state.CycleNext();
    }

    private void OnCyclePrev()
    {
        if (_state.GetMarkCount() == 0)
        {
            _trayIcon.ShowNotification("Winharpoon", "No marks to cycle through.", 1500);
            return;
        }
        _state.CyclePrev();
    }

    private void OnShowOverlay()
    {
        _marksOverlay.ShowOverlay();
    }

    private void OnClearAll()
    {
        if (_inMarkMode)
        {
            _state.ClearAll();
            _trayIcon.ShowNotification("Winharpoon", "All marks cleared.", 1500);
            ExitMarkMode();
        }
    }

    private void OnClearSlot(int slot)
    {
        _state.ClearSlot(slot);
        _trayIcon.ShowNotification("Winharpoon", $"Slot {slot} cleared.", 1500);
    }

    private void CheckMarkModeTimeout(object? sender, EventArgs e)
    {
        if (_inMarkMode && DateTime.UtcNow - _markModeStartTime > MarkModeTimeout)
        {
            ExitMarkMode();
        }
    }

    private void ExitMarkMode()
    {
        _inMarkMode = false;
        _markModeTimer.Stop();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _markModeTimer.Dispose();
        _hotkeyManager.Dispose();
        _trayIcon.Dispose();
        _hotkeyWindow.Dispose();
        _marksOverlay.Dispose();
        _disposed = true;
    }
}

internal class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    public event Action<int>? OnHotkey;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "WinharpoonHotkeyWindow",
            Style = 0,
            ExStyle = 0,
            Parent = IntPtr.Zero
        });
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            int hotkeyId = m.WParam.ToInt32();
            OnHotkey?.Invoke(hotkeyId);
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        DestroyHandle();
    }
}
