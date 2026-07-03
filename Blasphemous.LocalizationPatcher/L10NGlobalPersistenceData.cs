using Blasphemous.ModdingAPI.Persistence;
using System.Collections.Generic;

namespace Blasphemous.LocalizationPatcher;

internal class L10NGlobalPersistenceData : GlobalSaveData
{
    public Dictionary<string, List<string>> languageCodeToAppliedPatches = [];
    public Dictionary<string, List<string>> languageCodeToAppliedFonts = [];

    /// <summary>
    /// Language chosen when the game starts.
    /// </summary>
    public string languageOnStartup = "";

    /// <summary>
    /// Create a new List for new language code if one doesn't exist.
    /// </summary>
    internal void UpdateNewLanguageCodes()
    {
        List<string> languageNames = [];
        List<string> languageCodes = [];
        LocalizationPatcher.GetAllLanguageNamesAndCodes(ref languageNames, ref languageCodes);
        foreach (string languageCode in languageCodes)
        {
            if (!languageCodeToAppliedPatches.ContainsKey(languageCode))
            {
                languageCodeToAppliedPatches[languageCode] = [];
            }
            if (!languageCodeToAppliedFonts.ContainsKey(languageCode))
            {
                languageCodeToAppliedFonts[languageCode] = [];
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
