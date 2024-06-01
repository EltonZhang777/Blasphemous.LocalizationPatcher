using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Files;
using I2.Loc;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using UnityEngine.Windows.Speech;

namespace Blasphemous.Andalusian;

/// <summary>
/// Handles loading andalusian text and adding it to localization manager
/// </summary>
public class Andalusian : BlasMod
{
    internal Andalusian() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    private List<Language> languages = new List<Language>();

    /// <summary>
    /// Detects each language file in the `\data` folder.
    /// Then load and replace text for each language
    /// </summary>
    protected override void OnInitialize()
    {
        string[] allLanguageFilePaths = GetAllLanguageFilePaths();
        foreach (string filePath in allLanguageFilePaths)
        {
            languages.Add(new Language(filePath));
            Log($"language file at {filePath} detected");
        }

        foreach (Language language in languages)
        {
            LoadText(language);
            ReplaceText(language);
        }

        // display all current languages into log
        LogWarning($"All loaded languages:\n");
        LanguageSource source = LocalizationManager.Sources[0];
        List<string> allLanguageNames = source.GetLanguages();
        List<string> allLanguageCodes = source.GetLanguagesCode();
        int numCurrentLanguages = allLanguageNames.Count;
        for (int i = 0; i < numCurrentLanguages; i++)
        {
            Log($"#{i+1} loaded language-- \n language name: {allLanguageNames[i]}\n language code: {allLanguageCodes[i]}\n");
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

    private void ReplaceText(Language lang)
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
    /// Get all the language file paths in the `\data` directory. 
    /// </summary>
    private string[] GetAllLanguageFilePaths()
    {
        string dataPath = Path.GetFullPath(@"Modding/data/" + ModInfo.MOD_NAME + @"/");
        string[] allLanguageFilePaths = Directory.GetFiles(dataPath, "*.txt");
        return allLanguageFilePaths;
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