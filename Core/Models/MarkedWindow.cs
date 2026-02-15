namespace Winharpoon.Core.Models;

public class MarkedWindow
{
    public int Slot { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string TitlePattern { get; set; } = string.Empty;
    public IntPtr LastKnownHandle { get; set; }
}
