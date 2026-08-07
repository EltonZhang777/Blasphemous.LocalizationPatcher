using Blasphemous.ModdingAPI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Blasphemous.LocalizationPatcher.Components;

internal class SystemFontManager
{
    private string[] _cachedSystemFontNames;

    internal IEnumerable<string> AllSystemFonts => _cachedSystemFontNames ??= Font.GetOSInstalledFontNames();

    internal Dictionary<string, Font> loadedSystemFonts = [];

    /// <summary>
    /// Destroy all dynamically created system fonts and clear the cache.
    /// Call this when the mod is being unloaded.
    /// </summary>
    internal void UnloadAllSystemFonts()
    {
        foreach (Font font in loadedSystemFonts.Values)
        {
            if (font != null)
                UObject.Destroy(font);
        }
        loadedSystemFonts.Clear();
        _cachedSystemFontNames = null;
    }

    internal bool HasSystemFont(string fontName) => AllSystemFonts.Contains(fontName);
    internal bool HasLoadedSystemFont(string fontName) => TryGetLoadedSystemFont(fontName, out Font font) && (font != null);
    internal bool TryGetLoadedSystemFont(string fontName, out Font font)
    {
        return loadedSystemFonts.TryGetValue(fontName, out font);
    }

    internal bool TryLoadSystemFont(string fontName, int fontSize = 16)
    {
        // Prevent repeated loading
        if (loadedSystemFonts.Keys.Contains(fontName) && loadedSystemFonts[fontName] != null)
            return true;

        // Check if requested font name is in system fonts list
        if (!HasSystemFont(fontName))
            return false;

        Font font = Font.CreateDynamicFontFromOSFont(fontName, fontSize);
        if (font == null)
            return false;

        loadedSystemFonts.Add(fontName, font);
        return true;
    }

    internal bool TryApplySystemFont(string fontName, string langName)
    {
        // load the system font before using
        if (!TryLoadSystemFont(fontName))
            return false;

        if (!loadedSystemFonts.Keys.Contains(fontName))
            return false;

        CompiledLanguage targetCompiledLanguage = Main.LocalizationPatcher.compiledLanguages.FirstOrDefault(x => x.languageName == langName);
        if (targetCompiledLanguage == null)
        {
            ModLog.Warn($"Language `{langName}` not found when applying system font `{fontName}`!");
            return false;
        }

        // validate the font is applicable to the language
        // WIP, can probably use `Font.HasCharacter` or `Font.characterInfo`

        // apply the font to the specified language
        targetCompiledLanguage.ApplySystemFontToGame(fontName);

        return true;
    }
}
