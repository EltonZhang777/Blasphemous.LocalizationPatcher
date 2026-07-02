using Blasphemous.ModdingAPI.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blasphemous.LocalizationPatcher;

internal class L10NGlobalPersistenceData : GlobalSaveData
{
    internal Dictionary<string, List<string>> languageCodeToAppliedPatches = new();
    internal Dictionary<string, List<string>> languageCodeToAppliedFonts = new();

    /// <summary>
    /// Create a new List for new language code if one doesn't exist.
    /// </summary>
    internal void UpdateNewLanguageCodes()
    {
        List<string> languageNames = new();
        List<string> languageCodes = new();
        LocalizationPatcher.GetAllLanguageNamesAndCodes(ref languageNames, ref languageCodes);
        foreach (string languageCode in languageCodes)
        {
            if (!languageCodeToAppliedPatches.ContainsKey(languageCode))
            {
                languageCodeToAppliedPatches[languageCode] = new();
            }
            if (!languageCodeToAppliedFonts.ContainsKey(languageCode))
            {
                languageCodeToAppliedFonts[languageCode] = new();
            }
        }
    }

    internal void AddAppliedPatch(string languageCode, string patchName)
    {
        UpdateNewLanguageCodes();
        if (!languageCodeToAppliedPatches[languageCode].Contains(patchName))
        {
            languageCodeToAppliedPatches[languageCode].Add(patchName);
        }
    }

    internal void AddAppliedFont(string languageCode, string fontName)
    {
        UpdateNewLanguageCodes();
        if (!languageCodeToAppliedFonts[languageCode].Contains(fontName))
        {
            languageCodeToAppliedFonts[languageCode].Add(fontName);
        }
    }

    internal void RemoveAppliedPatch(string languageCode, string patchName)
    {
        UpdateNewLanguageCodes();
        if (languageCodeToAppliedPatches[languageCode].Contains(patchName))
        {
            languageCodeToAppliedPatches[languageCode].Remove(patchName);
        }
    }

    internal void RemoveAllAppliedPatches(string languageCode)
    {
        UpdateNewLanguageCodes();
        languageCodeToAppliedPatches[languageCode].Clear();
    }

    internal void RemoveAppliedFont(string languageCode, string fontName)
    {
        UpdateNewLanguageCodes();
        if (languageCodeToAppliedFonts[languageCode].Contains(fontName))
        {
            languageCodeToAppliedFonts[languageCode].Remove(fontName);
        }
    }

    internal void RemoveAllAppliedFonts(string languageCode)
    {
        UpdateNewLanguageCodes();
        languageCodeToAppliedFonts[languageCode].Clear();
    }
}
