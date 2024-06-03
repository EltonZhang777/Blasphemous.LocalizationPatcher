using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Files;
using I2.Loc;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace Blasphemous.LocalizationPatcher;

/// <summary>
/// Handles loading andalusian text and adding it to localization manager
/// </summary>
public class LocalizationPatcher : BlasMod
{
    internal LocalizationPatcher() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    private List<Language> languages = new List<Language>();

    /// <summary>
    /// the class containing config properties, loaded from `.cfg` file.
    /// </summary>
    public LanguageConfig config { get; private set; }

    /// <summary>
    /// Detects each language file in the `\data` folder.
    /// Then load and replace text for each language
    /// </summary>
    protected override void OnInitialize()
    {
        // load config
        config = ConfigHandler.Load<LanguageConfig>();

        List<string> allLanguageNames = new();
        List<string> allLanguageCodes = new();
        GetAllLanguageNamesAndCodes(ref allLanguageNames, ref allLanguageCodes);

        // first, remove all loaded languages in the game
        Log($"Removing all languages in vanilla game");
        foreach (string langName in allLanguageNames)
        {
            bool removeSuccessful = false;
            foreach (string langNameInGame in allLanguageNames)
            {
                if (string.Equals(langNameInGame.ToLower().Trim(), langName.ToLower().Trim()))
                {
                    RemoveLoadedLanguage(langName);
                    Log($"successfully removed language named \"{langName}\" from the game!");
                    removeSuccessful = true;
                    break;
                }
            }
            if (!removeSuccessful)
            {
                LogWarning($"failed disabling language: language named \"{langName}\" not found!");
            }
        }

        Log("Start loading all mod language files into the game");
        // load in all the language `.txt` patch files and construct their corresponding language objects.
        string[] allLanguageFilePaths = GetAllLanguageFilePaths();
        int currentLanguageIndex = 0;
        foreach (string filePath in allLanguageFilePaths)
        {
            string fileName = Path.GetFileName(filePath);
            languages.Add(new Language(filePath));
            if (config.disabledLanguages.Contains(languages[currentLanguageIndex].NAME))
            {
                Log($"Since language {languages[currentLanguageIndex].NAME} is disabled, " +
                    $"file `{languages[currentLanguageIndex].FILE_NAME}` is not loaded");
                languages.RemoveAt(currentLanguageIndex);
                continue;
            }
            Log($"Loaded language file {fileName}");
            currentLanguageIndex++;
        }

        // determine the patching order by reading config
        // check if all current patches are assigned a priority in the config file
        bool priorityAssigned = true;
        List<string> allPatchNames = ["base"];
        foreach (Language lang in languages)
        {
            if (!allPatchNames.Contains(lang.PATCH_NAME))
            {
                allPatchNames.Add(lang.PATCH_NAME);
            }
        }
        foreach (string patchName in allPatchNames)
        {
            if (!config.patchingOrder.Contains(patchName))
            {
                priorityAssigned = false;
                break;
            }
        }
        // if priority is not pre-assigned, assign priority
        // "base" gets smallest priority
        // other patches are assigned priority by the order in alphabetical order.
        // write the generated priority list into the config file
        if (!priorityAssigned)
        {
            LogWarning("Current patching order in config is not valid, resetting to default order.");
            config.patchingOrder = allPatchNames;
        }
        ConfigHandler.Save<LanguageConfig>(config);

        // load each langauge patch into the game, based on priority in config.
        // load the patches based on patching priority
        foreach (string patchName in config.patchingOrder)
        {
            foreach (Language language in languages.FindAll(lang => lang.PATCH_NAME == patchName))
            {
                LoadText(language);
                ReplaceText(language);
            }
        }

        // display all current languages into log
        GetAllLanguageNamesAndCodes(ref allLanguageNames, ref allLanguageCodes);
        Log($"Final summary of all loaded languages:");
        int numCurrentLanguages = allLanguageNames.Count;
        for (int i = 0; i < numCurrentLanguages; i++)
        {
            Log($"\nlanguage #{i + 1} : \n" +
                $"language name: {allLanguageNames[i]}\n" +
                $"language code: {allLanguageCodes[i]}");
            int currentPatchCount = 1;
            for (int j = 0; j < config.patchingOrder.Count; j++)
            {
                Language currentPatch = languages.Find(lang => lang.NAME == allLanguageNames[i] && lang.PATCH_NAME == config.patchingOrder[j]);
                if (currentPatch != null)
                {
                    Log($"#{currentPatchCount} patch for {allLanguageNames[i]}: {currentPatch.PATCH_NAME}");
                    currentPatchCount++;
                }
            }
        }
    }

    /// <summary>
    /// load all terms from the `.txt` file into a `Language` class object
    /// </summary>
    /// <param name="lang">language object being operated</param>
    private void LoadText(Language lang)
    {
        if (!FileHandler.LoadDataAsText(lang.FILE_NAME, out string text))
        {
            LogWarning($"Could not load the {lang.FILE_NAME} file!");
            return;
        }

        int nearEmptyTermCount = 0;
        foreach (string line in text.Split('\n'))
        {
            if (line.Length < 5)
            {
                nearEmptyTermCount++;
                continue;
            }
            // split each line into operation key, operationType, and value.
            string operationSeparator = "->";
            string valueSeparator = ":";
            int operationBeginIndex = line.IndexOf(operationSeparator);
            int valueBeginIndex = line.IndexOf(valueSeparator, operationBeginIndex + operationSeparator.Length);
            
            string key = line.Substring(0, operationBeginIndex - 0).Trim();
            string operationType = line.Substring(operationBeginIndex + operationSeparator.Length, valueBeginIndex - (operationBeginIndex + operationSeparator.Length)).Trim();
            string value = line.Substring(valueBeginIndex + valueSeparator.Length).Trim().Replace('@', '\n');

            // load  key, operationType, and value into dictionary objects of the language object
            if (value != string.Empty)
            {
                lang.TERM_KEYS.Add(key);
                lang.TERM_CONTENTS.Add(value);
                lang.TERM_OPERATIONS.Add(operationType);
            }
        }
        Log($"Successfully loaded {lang.TERM_KEYS.Count} terms of {lang.NAME} for patch `{lang.PATCH_NAME}`");
        if (nearEmptyTermCount > 0)
        {
            LogWarning($"Skipped {nearEmptyTermCount} near-empty terms\n");
        }
        else
        {
            Log($"All loadings are successful for this patch.\n");
        }
    }

    /// <summary>
    /// load all terms from the `Language` class object into the game
    /// </summary>
    /// <param name="lang">language object being operated</param>
    private void ReplaceText(Language lang)
    {
        // counting successful and failed operations
        int successfulCount = 0;
        int operationErrorCount = 0;
        int keyErrorCount = 0;
        // documenting whether a term isn't patched till the end due to its key being nonexistent.
        List<bool> keyErrorFlags = new List<bool>(Enumerable.Repeat(true, lang.TERM_KEYS.Count));

        foreach (LanguageSource source in LocalizationManager.Sources)
        {
            int languageIndex = source.GetLanguages().IndexOf(lang.NAME);
            if (languageIndex == -1) // register a new language to the game
            {
                source.AddLanguage(lang.NAME, lang.CODE);
                languageIndex = source.GetLanguages().IndexOf(lang.NAME);
            }
            List<string> allAvailableTerms = source.GetTermsList();
            for (int i = 0; i < lang.TERM_KEYS.Count; i++)
            {
                if (allAvailableTerms.Contains(lang.TERM_KEYS[i]))
                {
                    keyErrorFlags[i] = false;

                    string termOperation = lang.TERM_OPERATIONS[i];
                    string termKey = lang.TERM_KEYS[i];
                    string termText = lang.TERM_CONTENTS[i];
                    if (string.Equals(termOperation, "Replace"))
                    {
                        source.GetTermData(termKey).Languages[languageIndex] = termText;
                        successfulCount++;
                    }
                    else if (string.Equals(termOperation, "AppendAtBeginning"))
                    {
                        string originalTermText = source.GetTermData(termKey).Languages[languageIndex];
                        source.GetTermData(termKey).Languages[languageIndex] = termText + originalTermText;
                        successfulCount++;
                    }
                    else if (string.Equals(termOperation, "AppendAtEnd"))
                    {
                        string originalTermText = source.GetTermData(termKey).Languages[languageIndex];
                        source.GetTermData(termKey).Languages[languageIndex] = originalTermText + termText;
                        successfulCount++;
                    }
                    else
                    {
                        LogWarning($"term {termKey} calls for unsupported operation method {termOperation}, " +
                            $"skipping this term.");
                        operationErrorCount++;
                    }
                    
                }
            }
        }
        for ( int i = 0; i < keyErrorFlags.Count; i++ )
        {
            if (keyErrorFlags[i] == true)
            {
                LogWarning($"term key {lang.TERM_KEYS[i]} not found, skipping this term.");
                keyErrorCount++;
            }
        }

        Log($"Successfully patched {successfulCount} terms of {lang.NAME} from patch `{lang.PATCH_NAME}` into the game");
        if (keyErrorCount + operationErrorCount > 0)
        {
            LogWarning($"Skipped {keyErrorCount} terms with invalid keys " +
                $"and {operationErrorCount} terms with invalid operation type.\n");
        }
        else
        {
            Log($"All operations are successful for this patch.\n");
        }
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
    private void GetAllLanguageNamesAndCodes(ref List<string> names, ref List<string> codes)
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
        this.NAME = _languageNameCodeSplit[0].Trim();
        this.CODE = _languageNameCodeSplit[1].Trim();
        this.PATCH_NAME = _languageNameCodeSplit[2].Trim();
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
    /// The patch name of the language file.
    /// Patch name for language initialization files (containing all strings) is "base".
    /// </summary>
    public string PATCH_NAME;

    /// <summary>
    /// the full file path of the language's `.txt` file
    /// </summary>
    public string FULL_FILE_PATH;

    /// <summary>
    /// the file name path of the language's `.txt` file
    /// </summary>
    public string FILE_NAME;

    /// <summary>
    /// All the term keys of the language terms.
    /// </summary>
    public List<string> TERM_KEYS = new List<string>();

    /// <summary>
    /// All the term contents of the language terms.
    /// </summary>
    public List<string> TERM_CONTENTS = new List<string>();

    /// <summary>
    /// All the operations to be executed to the language terms.
    /// </summary>
    public List<string> TERM_OPERATIONS = new List<string>();
}