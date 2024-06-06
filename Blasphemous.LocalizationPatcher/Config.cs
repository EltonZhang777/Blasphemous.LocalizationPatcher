using System.Collections.Generic;

namespace Blasphemous.LocalizationPatcher;

/// <summary>
/// config class for LocalizationPatcher mod
/// </summary>
public class LanguageConfig
{
    /// <summary>
    /// list of all loaded languages' order in the game's settings.
    /// </summary>
    public List<string> languageOrder;

    /// <summary>
    /// a list of all disabled languages' language names.
    /// </summary>
    public List<string> disabledLanguages;

    /// <summary>
    /// a list of all loaded patches' patching order, from first to last.
    /// </summary>
    public List<string> patchingOrder;

    public List<string> disabledPatches;

    /// <summary>
    /// constructor of `LanguageConfig` object
    /// </summary>
    public LanguageConfig()
    {
        languageOrder = new List<string>();
        disabledLanguages = new List<string>();
        patchingOrder = new List<string>();
        disabledPatches = new List<string>(); 
    }
}
