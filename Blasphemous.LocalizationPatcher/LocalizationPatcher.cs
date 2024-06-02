using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Files;
using I2.Loc;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using UnityEngine.Windows.Speech;

namespace Blasphemous.LocalizationPatcher;

/// <summary>
/// Handles loading andalusian text and adding it to localization manager
/// </summary>
public class LocalizationPatcher : BlasMod
{
    internal LocalizationPatcher() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    private List<Language> languages = new List<Language>();

    public LanguageConfig config { get; private set; }

    /// <summary>
    /// Detects each language file in the `\data` folder.
    /// Then load and replace text for each language
    /// </summary>
    protected override void OnInitialize()
    {
        // load in all the custom language `.txt` files into language objects
        string[] allLanguageFilePaths = GetAllLanguageFilePaths();
        foreach (string filePath in allLanguageFilePaths)
        {
            languages.Add(new Language(filePath));
            Log($"language file at {filePath} detected");
        }

        // load each custom langauge into the game
        foreach (Language language in languages)
        {
            LoadText(language);
            ReplaceAllText(language);
        }

        // disable selected languages in the config file
        config = ConfigHandler.Load<LanguageConfig>();
        GetAllLanguageNamesAndCodes(out List<string> allLanguageNames, out List<string> allLanguageCodes);

        foreach (string langName in config.disabledLanguages)
        {
            bool replaceSuccessful = false;
            foreach (string langNameInGame in allLanguageNames)
            {
                if ( string.Equals(langNameInGame.ToLower().Trim(), langName.ToLower().Trim()) )
                {
                    RemoveLoadedLanguage(langName);
                    Log($"successfully removed language named \"{langName}\" from the game!");
                    replaceSuccessful = true;
                    break;
                }
            }
            if (!replaceSuccessful)
            {
                LogWarning($"failed disabling language: language named \"{langName}\" not found!");
            }
        }

        // display all current languages into log
        Log($"All loaded languages:\n");
        int numCurrentLanguages = allLanguageNames.Count;
        for (int i = 0; i < numCurrentLanguages; i++)
        {
            Log($"language[{i}] : \n language name: {allLanguageNames[i]}\n language code: {allLanguageCodes[i]}\n");
        }
    }


    private void LoadText(Language lang)
    {
        if (!FileHandler.LoadDataAsText(lang.FILE_NAME, out string text))
        {
            LogWarning($"Could not load the {lang.FILE_NAME} file!");
            return;
        }

        foreach (string line in text.Split('\n'))
        {
            int colonIdx = line.IndexOf(':');
            string key = line.Substring(0, colonIdx).Trim();
            string value = line.Substring(colonIdx + 1).Trim().Replace('@', '\n');

            if (value != string.Empty)
            {
                lang.CONTENTS.Add(key, value);
            }
        }
        Log($"loaded {lang.CONTENTS.Count} strings of {lang.NAME}");
    }

    private void ReplaceAllText(Language lang)
    {
        int count = 0;

        foreach (LanguageSource source in LocalizationManager.Sources)
        {
            source.AddLanguage(lang.NAME, lang.CODE);

            int lastLanguage = source.GetLanguages().Count - 1;
            foreach (string term in source.GetTermsList())
            {
                if (lang.CONTENTS.TryGetValue(term, out string newText))
                {
                    source.GetTermData(term).Languages[lastLanguage] = newText;
                    count++;
                }
            }
        }

        Log($"Successfully added {count} terms for {lang.NAME} translation");
    }

    /// <summary>
    /// remove the current language from the game.
    /// </summary>
    /// <param name="langName">name of language to remove</param>
    private void RemoveLoadedLanguage(string langName)
    {
        foreach (LanguageSource source in LocalizationManager.Sources)
        {
            source.RemoveLanguage(langName);
        }
    }

    

    /// <summary>
    /// Get all the language file paths in the `\data` directory. 
    /// </summary>
    private string[] GetAllLanguageFilePaths()
    {
        string dataPath = Path.GetFullPath(@"Modding/data/" + ModInfo.MOD_NAME + @"/");
        string[] allLanguageFilePaths = Directory.GetFiles(dataPath, "*.txt");
        return allLanguageFilePaths;
    }

    /// <summary>
    /// Get all the languages' (including vanilla languages) language names and codes into lists.
    /// </summary>
    /// <param name="names">all language names</param>
    /// <param name="codes">all language codes</param>
    private void GetAllLanguageNamesAndCodes(out List<string> names, out List<string> codes)
    {
        LanguageSource source = LocalizationManager.Sources[0];
        names = source.GetLanguages();
        codes = source.GetLanguagesCode();
    }
}

/// <summary>
/// contains the basic information of a language
/// </summary>
public class Language
{
    /// <summary>
    /// constructor of `Language` class
    /// </summary>
    /// <param name="fullFilePath"> the full file path of the language's `.txt` file</param>
    public Language(string fullFilePath)
    {
        this.FULL_FILE_PATH = fullFilePath;
        this.FILE_NAME = Path.GetFileName(fullFilePath);

        string[] _languageNameCodeSplit = Path.GetFileNameWithoutExtension(fullFilePath).Split('_');
        this.NAME = _languageNameCodeSplit[0];
        this.CODE = _languageNameCodeSplit[1];
    }

    /// <summary>
    /// the name of the language
    /// </summary>
    public string NAME;

    /// <summary>
    /// the internal language code of the language
    /// </summary>
    public string CODE;

    /// <summary>
    /// the full file path of the language's `.txt` file
    /// </summary>
    public string FULL_FILE_PATH;

    /// <summary>
    /// the file name path of the language's `.txt` file
    /// </summary>
    public string FILE_NAME;

    /// <summary>
    /// All the terms of the language as a dictionary.
    /// Key: term key; 
    /// Value: term text
    /// </summary>
    public Dictionary<string, string> CONTENTS = new Dictionary<string, string>();

}