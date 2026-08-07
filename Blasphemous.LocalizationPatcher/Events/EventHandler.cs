using Blasphemous.LocalizationPatcher.Components;

namespace Blasphemous.LocalizationPatcher.Events;

internal class EventHandler
{
    public delegate void FlagEvent(string flagId);

    public event FlagEvent OnFlagChange;

    public void FlagChange(string flagId)
    {
        OnFlagChange?.Invoke(LanguagePatch.FormatFlag(flagId));
    }
}
