using System.Text.Json;

namespace Winharpoon;

public class MarkedWindow
{
    public int Slot { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string TitlePattern { get; set; } = string.Empty;
    public IntPtr LastKnownHandle { get; set; }
}

public class HarpoonState
{
    private readonly Dictionary<int, MarkedWindow> _marks = new();
    private readonly string _configPath;
    private int _currentCycleIndex = -1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public event Action<int, MarkedWindow>? OnWindowMarked;
    public event Action<int>? OnWindowUnmarked;
    public event Action? OnAllCleared;

    public HarpoonState()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string winharpoonPath = Path.Combine(appDataPath, "Winharpoon");
        Directory.CreateDirectory(winharpoonPath);
        _configPath = Path.Combine(winharpoonPath, "config.json");

        Load();
    }

    public void MarkCurrentWindow(int slot)
    {
        if (slot < 1 || slot > 9)
            return;

        IntPtr hwnd = WindowManager.GetCurrentForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return;

        string? processName = WindowManager.GetProcessName(hwnd);
        string title = WindowManager.GetWindowTitle(hwnd);

        if (string.IsNullOrEmpty(processName) || string.IsNullOrEmpty(title))
            return;

        var mark = new MarkedWindow
        {
            Slot = slot,
            ProcessName = processName,
            TitlePattern = ExtractTitlePattern(title),
            LastKnownHandle = hwnd
        };

        _marks[slot] = mark;
        Save();

        OnWindowMarked?.Invoke(slot, mark);
    }

    public bool SwitchToSlot(int slot)
    {
        if (slot < 1 || slot > 9)
            return false;

        if (!_marks.TryGetValue(slot, out var mark))
            return false;

        // Try the last known handle first
        if (WindowManager.IsValidWindow(mark.LastKnownHandle))
        {
            return WindowManager.SwitchToWindow(mark.LastKnownHandle);
        }

        // Handle is no longer valid, try to find the window by process and title
        IntPtr? foundWindow = WindowManager.FindWindowByProcessAndTitle(mark.ProcessName, mark.TitlePattern);
        if (foundWindow.HasValue)
        {
            mark.LastKnownHandle = foundWindow.Value;
            Save();
            return WindowManager.SwitchToWindow(foundWindow.Value);
        }

        return false;
    }

    public void ClearSlot(int slot)
    {
        if (_marks.Remove(slot))
        {
            Save();
            OnWindowUnmarked?.Invoke(slot);
        }
    }

    public void ClearAll()
    {
        if (_marks.Count > 0)
        {
            _marks.Clear();
            _currentCycleIndex = -1;
            Save();
            OnAllCleared?.Invoke();
        }
    }

    public bool CycleNext()
    {
        var slots = _marks.Keys.OrderBy(k => k).ToList();
        if (slots.Count == 0) return false;

        _currentCycleIndex++;
        if (_currentCycleIndex >= slots.Count)
            _currentCycleIndex = 0;

        return SwitchToSlot(slots[_currentCycleIndex]);
    }

    public bool CyclePrev()
    {
        var slots = _marks.Keys.OrderBy(k => k).ToList();
        if (slots.Count == 0) return false;

        _currentCycleIndex--;
        if (_currentCycleIndex < 0)
            _currentCycleIndex = slots.Count - 1;

        return SwitchToSlot(slots[_currentCycleIndex]);
    }

    public int GetMarkCount() => _marks.Count;

    public IReadOnlyDictionary<int, MarkedWindow> GetMarks() => _marks;

    public MarkedWindow? GetMark(int slot)
    {
        return _marks.TryGetValue(slot, out var mark) ? mark : null;
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_configPath))
                return;

            string json = File.ReadAllText(_configPath);
            var savedMarks = JsonSerializer.Deserialize<List<SavedMark>>(json);

            if (savedMarks == null)
                return;

            foreach (var saved in savedMarks)
            {
                _marks[saved.Slot] = new MarkedWindow
                {
                    Slot = saved.Slot,
                    ProcessName = saved.ProcessName,
                    TitlePattern = saved.TitlePattern,
                    LastKnownHandle = IntPtr.Zero // Will be resolved on first switch
                };
            }
        }
        catch
        {
            // Ignore load errors, start fresh
        }
    }

    private void Save()
    {
        try
        {
            var savedMarks = _marks.Values.Select(m => new SavedMark
            {
                Slot = m.Slot,
                ProcessName = m.ProcessName,
                TitlePattern = m.TitlePattern
            }).ToList();

            string json = JsonSerializer.Serialize(savedMarks, JsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    private static string ExtractTitlePattern(string title)
    {
        // For most apps, the document/tab name is at the start
        // We'll use a reasonable substring for matching
        // Limit to first 50 chars or first separator
        const int maxLength = 50;

        int separatorIndex = title.IndexOfAny(['-', '|', '—', '–']);
        if (separatorIndex > 0 && separatorIndex < maxLength)
        {
            return title[..separatorIndex].Trim();
        }

        if (title.Length > maxLength)
        {
            return title[..maxLength];
        }

        return title;
    }

    private class SavedMark
    {
        public int Slot { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string TitlePattern { get; set; } = string.Empty;
    }
}
