namespace Blasphemous.LocalizationPatcher.Events;

internal class EventHandler
{
    public delegate void EventDelegate();

    public delegate void StandardEvent();
    public delegate void FlagEvent(string flagId);

    public event FlagEvent OnFlagChange;

    public void FlagChange(string flagId)
    {
        OnFlagChange?.Invoke(flagId);
    }
}
