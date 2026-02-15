namespace Winharpoon.Core.Interfaces;

public interface IStartupManager
{
    bool IsStartupEnabled();
    void SetStartupEnabled(bool enabled);
}
