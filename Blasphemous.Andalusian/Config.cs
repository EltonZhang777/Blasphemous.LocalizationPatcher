using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blasphemous.LocalizationPatcher;

public class LanguageConfig
{
    /// <summary>
    /// a list of all enabled languages' language names.
    /// </summary>
    public List<string> disabledLanguages;

    public LanguageConfig()
    {
        disabledLanguages = new List<string>();
    }
}
