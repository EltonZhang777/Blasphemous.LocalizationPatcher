using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blasphemous.LocalizationPatcher;

/// <summary>
/// config class for LocalizationPatcher mod
/// </summary>
public class LanguageConfig
{
    /// <summary>
    /// a list of all disabled languages' language names.
    /// </summary>
    public List<string> disabledLanguages;

    /// <summary>
    /// a list of all loaded languages' patching order, from first to last.
    /// </summary>
    public List<string> patchingOrder;

    /// <summary>
    /// constructor of `LanguageConfig` object
    /// </summary>
    public LanguageConfig()
    {
        disabledLanguages = new List<string>();
        patchingOrder = new List<string>();
    }
}
