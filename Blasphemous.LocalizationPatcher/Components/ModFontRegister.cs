using Blasphemous.ModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.LocalizationPatcher.Components;

/// <summary>
/// Register handler for new fonts
/// </summary>
public static class ModFontRegister
{
    private static readonly List<ModFont> _modFonts = [];
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
    /// Unload all registered mod fonts, releasing their AssetBundles and loaded assets,
    /// then clears the registry. Call this when the mod is being unloaded.
    /// </summary>
    internal static void UnloadAll()
    {
        foreach (ModFont modFont in _modFonts.ToList())
        {
            modFont.Unload();
        }
        _modFonts.Clear();
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

        // skip fonts that failed to load their assets (e.g. AssetBundle has no Font asset)
        if (modFont.ttfFont == null)
        {
            ModLog.Error($"Skipping mod font `{modFont?.info?.fontName}` because it has no valid Font asset.");
            return;
        }

        // prevents repeated registering
        string name = modFont.info.fontName;
        if (_modFonts.Any(x => x.info.fontName == name))
            return;

        modFont.parentModId = provider.RegisteringMod.Id;
        _modFonts.Add(modFont);
        ModLog.Info($"Registered custom ModFont: {modFont.info.fontName}");
    }
}

