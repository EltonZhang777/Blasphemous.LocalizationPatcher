using Blasphemous.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Gameplay.UI;
using I2.Loc;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public List<string> termKeys = [];

    /// <summary>
    /// All the prefixes of term contents of the language terms.
    /// </summary>
    public List<string> termPrefixes = [];

    /// <summary>
    /// All the central term contents of the language terms.
    /// </summary>
    public List<string> termContents = [];

    /// <summary>
    /// All the suffixes of term contents of the language terms.
    /// </summary>
    public List<string> termSuffixes = [];

    /// <summary>
    /// All patches that are applied to this language, in chronological order
    /// </summary>
    public List<string> patchesApplied = [];

    /// <summary>
    /// All mod fonts that are applicable to this language
    /// </summary>
    public List<ModFont> modFonts = [];

    /// <summary>
    /// The original term contents of the language terms, before any patches are applied.
    /// </summary>
    private List<string> _originalTermContentsBackup = [];

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
        termPrefixes = [.. Enumerable.Repeat(string.Empty, keyCount)];
        termContents = [.. Enumerable.Repeat(string.Empty, keyCount)];
        termSuffixes = [.. Enumerable.Repeat(string.Empty, keyCount)];
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
        int index = termKeys.IndexOf(termKey);
        if (index < 0)
        {
            ModLog.Warn($"Term key `{termKey}` is not registered in compiled language `{languageName}`, skipping this term.");
            return false;
        }

        bool result = false;

        foreach (LanguageSource source in I2LocManager.Sources)
        {
            List<string> allAvailableTerms = source.GetTermsList();
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
    /// Write selected terms of CompiledLanguage object into Blasphemous,
    /// then force localize the language to apply the updated terms.
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
        List<bool> keyErrorFlags = [.. Enumerable.Repeat(false, keys.Count)];

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

        // force localize the language in I2.Loc to apply the updated terms
        RefreshLocalizationLanguage();
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
    /// Remove a specific patch from this language by resetting all terms to original,
    /// then re-apply remaining patches in their original order.
    /// </summary>
    public void RemovePatchFromGame(string patchName)
    {
        if (!patchesApplied.Contains(patchName))
        {
            ModLog.Warn($"Cannot remove unapplied patch `{patchName}` from {languageName}!");
            return;
        }

        // Save remaining patches in original order (excluding the one to remove)
        List<string> remainingPatches = patchesApplied.Where(p => p != patchName).ToList();

        // Clear applied patches list (will be rebuilt by re-compilation)
        patchesApplied.Clear();

        // Reset all term data to original backup
        ResetTermsToOriginal();

        // Re-apply remaining patches in their original order
        foreach (string remainingPatchName in remainingPatches)
        {
            LanguagePatchRegister.AtName(remainingPatchName).CompileText();
        }

        // Write the final state to game
        WriteAllTermsToGame();

        // record change to save data
        RecordRemovedPatch(patchName);

        ModLog.Info($"Removed patch `{patchName}` from {languageName} and re-applied {remainingPatches.Count} remaining patches.");
    }

    /// <summary>
    /// Optimized implementation to write all patched terms to game
    /// </summary>
    public void WriteAllPatchesToGame()
    {
        // collect all modified term keys from all patches applied to this language
        List<string> allModifiedTermKeys = [];
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
        int index = termKeys.IndexOf(termKey);
        if (index < 0)
        {
            ModLog.Warn($"Term key `{termKey}` is not registered in compiled language `{languageName}`, skipping this term.");
            return false;
        }

        bool result = false;

        foreach (LanguageSource source in I2LocManager.Sources)
        {
            List<string> allAvailableTerms = source.GetTermsList();
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

        // backup the original term contents
        _originalTermContentsBackup = termContents.ToList();
    }

    /// <summary>
    /// Restore the original terms of this language to game's localization
    /// </summary>
    public void RestoreOriginalTermsToGame()
    {
        ResetTermsToOriginal();
        patchesApplied = [];
        // vanilla languages only write patched terms by default; since patchesApplied is now
        // cleared there are no patched terms left, so force-write ALL terms (which now contain
        // the restored original contents) to actually revert the game's localization.
        WriteAllTermsToGame(forceWriteAll: true);

        // record change to save data
        RemoveAllRecordedPatches();

        ModLog.Info($"Restored original terms of {languageName} to game.");
    }

    /// <summary>
    /// Reset all term data to original backup without writing to game or clearing patchesApplied.
    /// </summary>
    internal void ResetTermsToOriginal()
    {
        termContents = _originalTermContentsBackup.ToList();
        termPrefixes = [.. Enumerable.Repeat(string.Empty, termKeys.Count)];
        termSuffixes = [.. Enumerable.Repeat(string.Empty, termKeys.Count)];
    }

    #region Persistence recording methods

    internal void RecordAppliedPatch(string patchName)
    {
        Main.LocalizationPatcher.globalPersistenceData.AddAppliedPatch(languageCode, patchName);
    }

    internal void RecordRemovedPatch(string patchName)
    {
        Main.LocalizationPatcher.globalPersistenceData.RemoveAppliedPatch(languageCode, patchName);
    }

    internal void RemoveAllRecordedPatches()
    {
        Main.LocalizationPatcher.globalPersistenceData.RemoveAllAppliedPatches(languageCode);
    }

    internal void RecordAppliedFont(string fontName)
    {
        // because applying a font overwrites the previous one, remove all previous fonts first.
        RemoveAllRecordedFonts();

        Main.LocalizationPatcher.globalPersistenceData.AddAppliedFont(languageCode, fontName);
    }

    internal void RecordRemovedFont(string fontName)
    {
        Main.LocalizationPatcher.globalPersistenceData.RemoveAppliedFont(languageCode, fontName);
    }

    internal void RemoveAllRecordedFonts()
    {
        Main.LocalizationPatcher.globalPersistenceData.RemoveAllAppliedFonts(languageCode);
    }

    #endregion

    /// <summary>
    /// Apply the specified font to this language
    /// </summary>
    public void ApplyFontToGame(ModFont modFont)
    {
        if (!modFonts.Contains(modFont))
            return;

        // if modded fonts not found, use vanilla fonts
        string regularFontUsed;
        if (modFont.ttfFont != null)
        {
            regularFontUsed = modFont.TtfAssetName;
        }
        else if (IsVanillaLanguage)
        {
            regularFontUsed = LocalizationPatcher.vanillaRegularFontNames[languageName];
            ModLog.Error($"Modded regular font `{modFont?.TtfAssetName}` does not exist for language {languageName}, using default font `{regularFontUsed}`.");
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
            ModLog.Error($"Modded TextMeshPro font `{modFont?.TmpAssetName}` does not exist for language {languageName}, using default font `{tmpFontUsed}`.");
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
        RefreshLocalizationLanguage();

        // record change to save data
        RecordAppliedFont(modFont.info.fontName);
    }

    /// <summary>
    /// Apply specified system font (installed in the user's computer) to this language
    /// </summary>
    public void ApplySystemFontToGame(string fontName)
    {
        if (!Main.LocalizationPatcher.SystemFontManager.HasLoadedSystemFont(fontName))
            return;

        // update the fonts to I2.Loc manager
        TryUpdateTerm("UI/FONT", fontName, PatchTerm.TermOperation.ReplaceAll);
        TryUpdateTerm("UI/FONT_SCROLL", fontName, PatchTerm.TermOperation.ReplaceAll);

        WriteTermsToGame(["UI/FONT", "UI/FONT_SCROLL"]);

        // force localize the language in I2.Loc to apply the font
        RefreshLocalizationLanguage();

        // record change to save data
        RecordAppliedFont(fontName);
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
        LanguageSource source = I2LocManager.Sources[0];
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

    /// <summary>
    /// Refresh current game language to update localization. 
    /// </summary>
    internal void RefreshLocalizationLanguage()
    {
        // if the game is NOT currently using the language being patched, it will be updated next time the player switch to this language, so nothing to do now.
        if (I2LocManager.CurrentLanguage != languageName)
            return;

        ModLogExtensions.WarnIfDebugBuild($"Refreshing language `{languageName}` to update localization.");
        // refresh current language by switching to another language and switch back
        // determine the parent for executing coroutine. Use any MonoBehaviour as fallback for UIController.
        MonoBehaviour coroutineParent = UIController.instance ?? UObject.FindObjectOfType<MonoBehaviour>();
        if (I2LocManager.CurrentLanguage != "English")
        {
            // if current language isn't English, switch to English and switch back
            coroutineParent.StartCoroutine(SwitchToTargetLanguageAndSwitchBack("English"));
        }
        else
        {
            // current language is English, switch to Chinese and switch back
            coroutineParent.StartCoroutine(SwitchToTargetLanguageAndSwitchBack("Chinese"));
        }
    }

    /// <summary>
    /// Switch to target language and switch back to current language to refresh localization. 
    /// </summary>
    private IEnumerator SwitchToTargetLanguageAndSwitchBack(string targetLanguage)
    {
        yield return new WaitForEndOfFrame();
        I2LocManager.SetLanguageAndCode(targetLanguage, I2LocManager.GetLanguageCode(targetLanguage), true, true);
        yield return new WaitForEndOfFrame();
        I2LocManager.SetLanguageAndCode(languageName, I2LocManager.GetLanguageCode(languageName), true, true);
    }
}