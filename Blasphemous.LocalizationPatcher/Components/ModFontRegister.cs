using Blasphemous.ModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.LocalizationPatcher.Components;

/// <summary>
/// Register handler for new fonts
/// </summary>
public static class ModFontRegister
{
    private static readonly List<ModFont> _modFonts = new();
    internal static IEnumerable<ModFont> ModFonts => _modFonts;
    internal static int Total => _modFonts.Count;
    internal static ModFont AtIndex(int index) => _modFonts[index];
    internal static ModFont AtName(string name)
    {
        try
        {
            return _modFonts.First(x => x.info.fontName == name);
        }
        catch
        {
            ModLog.Warn($"Queried nonexistent patch `{name}`");
            return null;
        }
    }

    /// <summary>
    /// Registers a new ModFont 
    /// </summary>
    public static void RegisterModFont(
        this ModServiceProvider provider,
        ModFont modFont)
    {
        if (provider == null)
            return;

        // prevents repeated registering
        string name = modFont.info.fontName;
        if (_modFonts.Any(x => x.info.fontName == name))
            return;

        modFont.parentModId = provider.RegisteringMod.Id;
        _modFonts.Add(modFont);
        ModLog.Info($"Registered custom ModFont: {modFont.info.fontName}");
    }
}

