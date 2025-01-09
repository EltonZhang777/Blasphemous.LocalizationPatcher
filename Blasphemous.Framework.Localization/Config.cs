using System.Collections.Generic;

namespace Blasphemous.Framework.Localization;

/// <summary>
/// Master config class for LocalizationFramework mod
/// </summary>
public class Config
{
    /// <summary>
    /// List of all loaded languages' order in the game's settings.
    /// </summary>
    public List<string> languageOrder = new();

    /// <summary>
    /// List of all disabled languages' language names.
    /// </summary>
    public List<string> disabledLanguages = new();

    /// <summary>
    /// List of patching order of all mods, from first to last.
    /// </summary>
    public List<string> patchingModOrder = new();

    /// <summary>
    /// List of patches disabled in the config file
    /// </summary>
    public List<string> disabledPatches = new();
}
