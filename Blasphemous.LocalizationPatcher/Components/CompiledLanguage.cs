using Blasphemous.ModdingAPI;
using I2.Loc;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.LocalizationPatcher.Components;

/// <summary>
/// Contains the information of each language (one object per language, not per patch). 
/// </summary>
public class CompiledLanguage
{
    /// <summary>
    /// The name of the language
    /// </summary>
    public string languageName;

    /// <summary>
    /// The internal language code of the language in `I2.Loc`
    /// </summary>
    public string languageCode;

    /// <summary>
    /// All the term keys of the language terms.
    /// </summary>
    public List<string> termKeys = new();

    /// <summary>
    /// All the prefixes of term contents of the language terms.
    /// </summary>
    public List<string> termPrefixes = new();

    /// <summary>
    /// All the central term contents of the language terms.
    /// </summary>
    public List<string> termContents = new();

    /// <summary>
    /// All the suffixes of term contents of the language terms.
    /// </summary>
    public List<string> termSuffixes = new();

    /// <summary>
    /// All patches that are applied to this language, in chronological order
    /// </summary>
    public List<string> patchesApplied = new();

    /// <summary>
    /// All mod fonts that are applicable to this language
    /// </summary>
    public List<ModFont> modFonts = new();

    private int _languageIndex = -1;

    internal bool IsVanillaLanguage => LocalizationPatcher.IsVanillaLanguage(languageName);

    /// <summary>
    /// Constructor of the CompiledLanguage class. 
    /// </summary>
    /// <param name="langName"> name of the language </param>
    /// <param name="langCode"> language code of the language </param>
    public CompiledLanguage(string langName, string langCode)
    {
        languageName = langName;
        languageCode = langCode;

        termKeys = Main.LocalizationPatcher.allPossibleKeys;
        int keyCount = termKeys.Count;
        termPrefixes = new(Enumerable.Repeat(string.Empty, keyCount));
        termContents = new(Enumerable.Repeat(string.Empty, keyCount));
        termSuffixes = new(Enumerable.Repeat(string.Empty, keyCount));
    }

    /// <summary>
    /// Update a term in the CompiledLanguage object. 
    /// Returns false if an error occured.
    /// </summary>
    public bool TryUpdateTerm(string key, string text, PatchTerm.TermOperation operation)
    {

        // if the key isn't registered, it must be an invalid key
        if (!termKeys.Contains(key))
        {
            ModLog.Error($"Term key `{key}` is invalid!");
            return false;
        }
        int termIndex = termKeys.IndexOf(key);

        if (operation == PatchTerm.TermOperation.Replace)
        {
            termContents[termIndex] = text;
        }
        else if (operation == PatchTerm.TermOperation.ReplaceAll)
        {
            termContents[termIndex] = text;
            RemovePrefixAndSuffix(key);
        }
        else if (operation == PatchTerm.TermOperation.Prefix)
        {
            termPrefixes[termIndex] = text + termPrefixes[termIndex];
        }
        else if (operation == PatchTerm.TermOperation.Suffix)
        {
            termSuffixes[termIndex] = termSuffixes[termIndex] + text;
        }
        else
        {
            ModLog.Error($"Term of key `{key}` calls for unsupported operation method {operation}, skipping this term.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Update a term in the CompiledLanguage object. 
    /// Returns false if an error occured.
    /// </summary>
    public bool TryUpdateTerm(string key, string text, string operation)
    {
        return TryUpdateTerm(key, text, PatchTerm.ParseToTermOperation(operation));
    }

    /// <summary>
    /// Update a term in the CompiledLanguage object. 
    /// Returns false if an error occured.
    /// </summary>
    public bool TryUpdateTerm(PatchTerm term)
    {
        return TryUpdateTerm(term.termKey, term.termContent, term.termOperation);
    }

    /// <summary>
    /// load a specific term of CompiledLanguage object into Blasphemous, 
    /// returns false if process failed
    /// </summary>
    /// <param name="termKey">Key of the term that needs to be updated</param>
    public bool TryWriteTermToGame(string termKey)
    {
        bool result = false;

        foreach (LanguageSource source in I2LocManager.Sources)
        {
            List<string> allAvailableTerms = source.GetTermsList();
            int index = termKeys.IndexOf(termKey);
            if (allAvailableTerms.Contains(termKey))
            {
                result = true;
                string termText = termPrefixes[index] + termContents[index] + termSuffixes[index];
                source.GetTermData(termKey).Languages[_languageIndex] = termText;
            }
        }

        return result;
    }

    /// <summary>
    /// Write selected terms of CompiledLanguage object into Blasphemous
    /// </summary>
    public void WriteTermsToGame(List<string> keys)
    {
        UpdateLanguageIndex();
        if (keys.Count == 0)
            return;

        int successfulCount = 0;
        int keyErrorCount = 0;

        // documenting whether a term isn't patched till the end due to its key being nonexistent.
        // true => this term has keyError
        List<bool> keyErrorFlags = new List<bool>(Enumerable.Repeat(false, keys.Count));

        foreach (LanguageSource source in I2LocManager.Sources)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (!TryWriteTermToGame(keys[i]))
                {
                    keyErrorFlags[i] = true;
                }
            }
        }

        // Error logging
        for (int i = 0; i < keyErrorFlags.Count; i++)
        {
            if (keyErrorFlags[i] == true)
            {
                ModLog.Warn($"term key {keys[i]} not found in Blasphemous localization directory, " +
                    $"skipping this term.");
                keyErrorCount++;
            }
            else
            {
                successfulCount++;
            }
        }
        ModLog.Info($"Successfully updated {successfulCount} of {keys.Count} terms of {languageName} into the game");
        if (keyErrorCount > 0)
        {
            ModLog.Warn($"Skipped {keyErrorCount} terms with invalid keys.\n");
        }
        else
        {
            ModLog.Info($"Update process encountered no error.\n");
        }
    }

    /// <summary>
    /// Write all terms which are modified by specific patch to game
    /// </summary>
    /// <param name="patchName"></param>
    public void WritePatchToGame(string patchName)
    {
        if (!patchesApplied.Contains(patchName))
        {
            ModLog.Warn($"Failed attempting to write unapplied patch `{patchName}` to game!");
            return;
        }

        WriteTermsToGame(LanguagePatchRegister.AtName(patchName).patchTerms.Select(x => x.termKey).ToList());
    }

    /// <summary>
    /// Optimized implementation to write all patched terms to game
    /// </summary>
    public void WriteAllPatchesToGame()
    {
        // collect all modified term keys from all patches applied to this language
        List<string> allModifiedTermKeys = new();
        foreach (string patchName in patchesApplied)
        {
            allModifiedTermKeys.AddRange(LanguagePatchRegister.AtName(patchName).patchTerms.Select(x => x.termKey));
        }
        allModifiedTermKeys = allModifiedTermKeys.Distinct().ToList();

        // write only those terms to the game
        WriteTermsToGame(allModifiedTermKeys);
    }

    /// <summary>
    /// Optimized implementation to write all terms of this CompiledLanguage object to game's localization
    /// </summary>
    public void WriteAllTermsToGame(bool forceWriteAll = false)
    {
        if (!IsVanillaLanguage || forceWriteAll) // if this is not a vanilla language or forceWriteAll is true, all terms must be written.
        {
            WriteTermsToGame(termKeys);
        }
        else // for vanilla languages, only write terms that are patched
        {
            WriteAllPatchesToGame();
        }
    }

    /// <summary>
    /// load a specific term from the game to CompiledLanguage object, 
    /// returns false if process failed
    /// </summary>
    /// <param name="termKey">Key of the term that needs to be updated</param>
    public bool TryReadTermFromGame(string termKey)
    {
        bool result = false;

        foreach (LanguageSource source in I2LocManager.Sources)
        {
            List<string> allAvailableTerms = source.GetTermsList();
            int index = termKeys.IndexOf(termKey);
            if (allAvailableTerms.Contains(termKey))
            {
                termContents[index] = source.GetTermData(termKey).Languages[_languageIndex];
                result = true;
            }
        }

        return result;
    }

    /// <summary>
    /// load all translation terms in game to this object
    /// </summary>
    public void ReadAllTermsFromGame()
    {
        UpdateLanguageIndex();
        for (int i = 0; i < termKeys.Count; i++)
        {
            bool success = false;
            foreach (LanguageSource source in I2LocManager.Sources)
            {
                success |= TryReadTermFromGame(termKeys[i]);
            }

            if (!success)
            {
                ModLog.Warn($"Error loading term {termKeys[i]} from language {languageName}");
            }
        }
    }

    /// <summary>
    /// Apply the specified font to this language
    /// </summary>
    public void ApplyFontToGame(ModFont modFont)
    {
        if (!modFonts.Contains(modFont))
            return;

        // if modded fonts not found, use vanilla fonts
        string regularFontUsed;
        if (modFont.regularFont != null)
        {
            regularFontUsed = modFont.RegularAssetName;
        }
        else if (IsVanillaLanguage)
        {
            regularFontUsed = LocalizationPatcher.vanillaRegularFontNames[languageName];
            ModLog.Error($"Modded regular font `{modFont}` does not exist for language {languageName}, using default font `{regularFontUsed}`.");
        }
        else
        {
            regularFontUsed = "MajesticExtended_Pixel_Scroll";
            ModLog.Error($"No default font found for language {languageName}, using default English font `{regularFontUsed}`.");
        }

        string tmpFontUsed;
        if (modFont.tmpFont != null)
        {
            tmpFontUsed = modFont.TmpAssetName;
        }
        else if (IsVanillaLanguage)
        {
            tmpFontUsed = LocalizationPatcher.vanillaTmpFontNames[languageName];
            ModLog.Error($"Modded TextMeshPro font `{modFont}` does not exist for language {languageName}, using default font `{tmpFontUsed}`.");
        }
        else
        {
            tmpFontUsed = "MajesticExtended_FullLatin";
            ModLog.Error($"No default font found for language {languageName}, using default English font `{tmpFontUsed}`.");
        }

        // update the fonts to I2.Loc manager
        TryUpdateTerm("UI/FONT", regularFontUsed, PatchTerm.TermOperation.ReplaceAll);
        TryUpdateTerm("UI/FONT_SCROLL", regularFontUsed, PatchTerm.TermOperation.ReplaceAll);
        TryUpdateTerm("UI/FONT_TEXTMESH_PRO", tmpFontUsed, PatchTerm.TermOperation.ReplaceAll);

        WriteTermsToGame(["UI/FONT", "UI/FONT_SCROLL", "UI/FONT_TEXTMESH_PRO"]);

        // force localize the language in I2.Loc to apply the font
        I2LocManager.SetLanguageAndCode(languageName, I2LocManager.GetLanguageCode(languageName), true, true);
    }

    /// <summary>
    /// Get the fonts currently used by this language
    /// </summary>
    public void GetCurrentFonts(out string regularFontUsed, out string tmpFontUsed)
    {
        // get the current fonts used in I2.Loc
        regularFontUsed = I2LocManager.GetTermData("UI/FONT").Languages[_languageIndex];
        tmpFontUsed = I2LocManager.GetTermData("UI/FONT_TEXTMESH_PRO").Languages[_languageIndex];
    }

    /// <summary>
    /// Updates the index of the langauge in the LocalizationManager to this object, 
    /// or register a new language if the specified langauge is not found.
    /// </summary>
    internal void UpdateLanguageIndex()
    {
        var source = I2LocManager.Sources[0];
        int index = source.GetLanguageIndex(languageName);
        if (index == -1)
        {
            LocalizationPatcher.AddLanguageToGame(languageName, languageCode);
            index = source.GetLanguageIndex(languageName);
        }
        _languageIndex = index;
    }

    internal void RemovePrefixAndSuffix(string termKey)
    {
        int termIndex = termKeys.IndexOf(termKey);
        termPrefixes[termIndex] = string.Empty;
        termSuffixes[termIndex] = string.Empty;
    }
}