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

    private int languageIndex = -1;

    /// <summary>
    /// Constructor of the CompiledLanguage class. 
    /// Automatically registers the langauge to game if not found.
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

        GetAndUpdateLanguageIndex();
    }

    /// <summary>
    /// Update a term in the CompiledLanguage object. 
    /// Returns false if an error occured.
    /// </summary>
    /// <param name="key"> key of the term being updated </param>
    /// <param name="text"> text of the term </param>
    /// <param name="operation"> operation of the term</param>
    public bool TryUpdateTerm(string key, string text, string operation)
    {

        // if the key isn't registered, register the key and all 3 types of contents
        if (!termKeys.Contains(key))
        {
            termKeys.Add(key);
            termPrefixes.Add(string.Empty);
            termContents.Add(string.Empty);
            termSuffixes.Add(string.Empty);
        }
        int termIndex = termKeys.IndexOf(key);

        if (operation.Equals("Replace"))
        {
            termContents[termIndex] = text;
        }
        else if (operation.Equals("ReplaceAll"))
        {
            termContents[termIndex] = text;
            RemovePrefixAndSuffix(key);
        }
        else if (operation.Equals("AppendAtBeginning") || operation.Equals("Prefix"))
        {
            termPrefixes[termIndex] = text + termPrefixes[termIndex];
        }
        else if (operation.Equals("AppendAtEnd") || operation.Equals("Suffix"))
        {
            termSuffixes[termIndex] = termSuffixes[termIndex] + text;
        }
        else
        {
            ModLog.Error($"term {key} calls for unsupported operation method {operation}, " +
                            $"skipping this term.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// load a specific term of CompiledLanguage object into Blasphemous, 
    /// returns false if process failed
    /// </summary>
    /// <param name="termKey">Key of the term that needs to be updated</param>
    public bool TryWriteTermToGame(string termKey)
    {
        bool result = false;

        foreach (LanguageSource source in LocalizationManager.Sources)
        {
            List<string> allAvailableTerms = source.GetTermsList();
            int index = termKeys.IndexOf(termKey);
            if (allAvailableTerms.Contains(termKey))
            {
                result = true;
                string termText = termPrefixes[index] + termContents[index] + termSuffixes[index];
                source.GetTermData(termKey).Languages[languageIndex] = termText;
            }
        }

        return result;
    }

    /// <summary>
    /// load all the terms of CompiledLanguage object into Blasphemous
    /// </summary>
    public void WriteAllTermsToGame()
    {
        int successfulCount = 0;
        int keyErrorCount = 0;

        // documenting whether a term isn't patched till the end due to its key being nonexistent.
        // true => this term has keyError
        List<bool> keyErrorFlags = new List<bool>(Enumerable.Repeat(true, termKeys.Count));

        foreach (LanguageSource source in I2.Loc.LocalizationManager.Sources)
        {
            for (int i = 0; i < termKeys.Count; i++)
            {
                if (TryWriteTermToGame(termKeys[i]))
                {
                    keyErrorFlags[i] = false;
                }
            }
        }

        // Error logging
        for (int i = 0; i < keyErrorFlags.Count; i++)
        {
            if (keyErrorFlags[i] == true)
            {
                ModLog.Warn($"term key {termKeys[i]} not found in Blasphemous localization directory, " +
                    $"skipping this term.");
                keyErrorCount++;
            }
            else
            {
                successfulCount++;
            }
        }
        ModLog.Info($"Successfully updated {successfulCount} of {termKeys.Count} terms of {languageName} into the game");
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
    /// load a specific term from the game to CompiledLanguage object, 
    /// returns false if process failed
    /// </summary>
    /// <param name="termKey">Key of the term that needs to be updated</param>
    public bool TryReadTermFromGame(string termKey)
    {
        bool result = false;

        foreach (LanguageSource source in LocalizationManager.Sources)
        {
            List<string> allAvailableTerms = source.GetTermsList();
            int index = termKeys.IndexOf(termKey);
            if (allAvailableTerms.Contains(termKey))
            {
                termContents[index] = source.GetTermData(termKey).Languages[languageIndex];
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
        GetAndUpdateLanguageIndex();
        for (int i = 0; i < termKeys.Count; i++)
        {
            bool success = false;
            foreach (LanguageSource source in I2.Loc.LocalizationManager.Sources)
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
    /// Updates the index of the langauge in the LocalizationManager to this object, 
    /// or register a new language if the specified langauge is not found.
    /// </summary>
    internal void GetAndUpdateLanguageIndex()
    {
        var source = LocalizationManager.Sources[0];
        int index = source.GetLanguageIndex(languageName);
        if (index == -1)
        {
            LocalizationPatcher.AddLanguageToGame(languageName, languageCode);
            index = source.GetLanguageIndex(languageName);
        }
        languageIndex = index;
    }

    internal void RemovePrefixAndSuffix(string termKey)
    {
        int termIndex = termKeys.IndexOf(termKey);
        termPrefixes[termIndex] = string.Empty;
        termSuffixes[termIndex] = string.Empty;
    }
}