using System.Drawing;

namespace Winharpoon;

public class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly HarpoonState _state;
    private readonly ToolStripMenuItem _marksMenuItem;
    private readonly ToolStripMenuItem _startupMenuItem;
    private bool _disposed;

    public event Action? OnExitRequested;

    public TrayIcon(HarpoonState state)
    {
        _state = state;

        _contextMenu = new ContextMenuStrip();

        // Marks submenu
        _marksMenuItem = new ToolStripMenuItem("Marks");
        _contextMenu.Items.Add(_marksMenuItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Run at startup toggle
        _startupMenuItem = new ToolStripMenuItem("Run at startup")
        {
            Checked = StartupManager.IsStartupEnabled(),
            CheckOnClick = true
        };
        _startupMenuItem.Click += (_, _) =>
        {
            StartupManager.SetStartupEnabled(_startupMenuItem.Checked);
        };
        _contextMenu.Items.Add(_startupMenuItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Hotkeys help submenu
        var helpMenu = new ToolStripMenuItem("Hotkeys");
        helpMenu.DropDownItems.Add(new ToolStripMenuItem("Ctrl+Shift+M, 1-9: Mark window") { Enabled = false });
        helpMenu.DropDownItems.Add(new ToolStripMenuItem("Ctrl+Shift+M, 0: Clear all marks") { Enabled = false });
        helpMenu.DropDownItems.Add(new ToolStripMenuItem("Ctrl+Shift+1-9: Switch to window") { Enabled = false });
        helpMenu.DropDownItems.Add(new ToolStripMenuItem("Ctrl+Shift+N/P: Cycle next/prev") { Enabled = false });
        helpMenu.DropDownItems.Add(new ToolStripMenuItem("Ctrl+Shift+H: Show overlay") { Enabled = false });
        _contextMenu.Items.Add(helpMenu);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => OnExitRequested?.Invoke();
        _contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateHarpoonIcon(),
            Visible = true,
            Text = "Winharpoon",
            ContextMenuStrip = _contextMenu
        };

        // Subscribe to state changes
        _state.OnWindowMarked += OnWindowMarked;
        _state.OnWindowUnmarked += OnWindowUnmarked;

        // Initial update
        UpdateMarksMenu();
    }

    private void OnWindowMarked(int slot, MarkedWindow mark)
    {
        UpdateMarksMenu();
    }

    private void OnWindowUnmarked(int slot)
    {
        UpdateMarksMenu();
    }

    private void UpdateMarksMenu()
    {
        _marksMenuItem.DropDownItems.Clear();

        var marks = _state.GetMarks();
        if (marks.Count == 0)
        {
            var emptyItem = new ToolStripMenuItem("(No marks)")
            {
                Enabled = false
            };
            _marksMenuItem.DropDownItems.Add(emptyItem);
            return;
        }

        foreach (var kvp in marks.OrderBy(x => x.Key))
        {
            var mark = kvp.Value;
            string displayText = $"{mark.Slot}: {mark.ProcessName} - {Truncate(mark.TitlePattern, 30)}";

            var item = new ToolStripMenuItem(displayText);

            // Add clear option
            var clearItem = new ToolStripMenuItem("Clear");
            int slot = mark.Slot;
            clearItem.Click += (_, _) => _state.ClearSlot(slot);
            item.DropDownItems.Add(clearItem);

            _marksMenuItem.DropDownItems.Add(item);
        }
    }

    public void ShowNotification(string title, string message, int durationMs = 2000)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(durationMs);
    }

    public void ShowMarkModeNotification()
    {
        ShowNotification("Winharpoon", "Press 1-9 to mark current window to slot", 2000);
    }

    public void ShowMarkedNotification(int slot, string processName)
    {
        ShowNotification("Winharpoon", $"Window marked to slot {slot} ({processName})", 1500);
    }

    public void ShowSwitchFailedNotification(int slot)
    {
        ShowNotification("Winharpoon", $"No window found for slot {slot}", 1500);
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;
        return text[..(maxLength - 3)] + "...";
    }

    private static Icon CreateHarpoonIcon()
    {
        // Create a simple 16x16 icon with an "H" for Harpoon
        using var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.Clear(Color.FromArgb(64, 64, 64));

        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);

        var textSize = graphics.MeasureString("H", font);
        float x = (16 - textSize.Width) / 2;
        float y = (16 - textSize.Height) / 2;

        graphics.DrawString("H", font, brush, x, y);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _state.OnWindowMarked -= OnWindowMarked;
        _state.OnWindowUnmarked -= OnWindowUnmarked;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();

        _disposed = true;
    }
}
