using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Files;
using I2.Loc;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Blasphemous.LocalizationPatcher;

/// <summary>
/// Handles loading andalusian text and adding it to localization manager
/// </summary>
public class LocalizationPatcher : BlasMod
{
    internal LocalizationPatcher() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    private List<LanguagePatch> languagePatches = new List<LanguagePatch>();

    private List<CompiledLanguage> compiledLanguages = new List<CompiledLanguage>();

    /// <summary>
    /// all terms keys in Blasphemous' localization service.
    /// </summary>
    private List<string> allPossibleKeys = new List<string>();

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

        // read all the term keys from base game
        foreach (LanguageSource source in LocalizationManager.Sources)
        {
            allPossibleKeys.AddRange(source.GetTermsList());
        }

        // load in all the language `.txt` patch files
        //   and construct their corresponding LanguagePatch objects.
        // skip all disabled languages and disabled patches
        Log("Start loading all mod language files into the game");
        string[] allLanguageFilePaths = GetAllLanguageFilePaths();
        int currentLanguageIndex = 0;
        foreach (string filePath in allLanguageFilePaths)
        {
            string fileName = Path.GetFileName(filePath);
            languagePatches.Add(new LanguagePatch(filePath));
            if (config.disabledLanguages.Contains(languagePatches[currentLanguageIndex].NAME))
            {
                Log($"Since language {languagePatches[currentLanguageIndex].NAME} is disabled, " +
                    $"file `{languagePatches[currentLanguageIndex].FILE_NAME}` is not loaded");
                languagePatches.RemoveAt(currentLanguageIndex);
                continue;
            }
            else if (config.disabledPatches.Contains(languagePatches[currentLanguageIndex].PATCH_NAME))
            {
                Log($"Since patch {languagePatches[currentLanguageIndex].PATCH_NAME} is disabled, " +
                    $"file `{languagePatches[currentLanguageIndex].FILE_NAME}` is not loaded");
                languagePatches.RemoveAt(currentLanguageIndex);
                continue;
            }
            Log($"Loaded language file {fileName}");
            currentLanguageIndex++;
        }

        // determine the patching order by reading config.
        // check if all current patches are assigned a priority in the config file
        bool patchingPriorityAssigned = true;
        List<string> allPatchNames = ["base"];
        foreach (LanguagePatch lang in languagePatches)
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
                patchingPriorityAssigned = false;
                config.patchingOrder.Add(patchName);
            }
        }
        // if priority is not pre-assigned, or the first patch is not `base`, assign priority
        // "base" gets smallest priority
        // patches not assigned priority originally are given largest priority value
        // multiple unassigned patches are assigned by `.txt` read-in order.
        if (!string.Equals(config.patchingOrder[0].Trim(), "base"))
        {
            LogWarning($"Must patch `base` patches first. Resetting patching order to default.");
            config.patchingOrder = allPatchNames;
        }
        else if (!patchingPriorityAssigned)
        {
            LogWarning("Missing some patches in the current patching order, " +
                "unassigned patches are patched at the end.");
        }
        // save current config into the config file
        ConfigHandler.Save<LanguageConfig>(config);

        // Load each langauge patch into CompiledLanguage object of corresponding language,
        //   based on priority in config.
        foreach (string patchName in config.patchingOrder)
        {
            foreach (LanguagePatch language in languagePatches.FindAll(lang => lang.PATCH_NAME == patchName))
            {
                LoadText(language);
                CompileText(language);
            }
        }

        // remove all vanilla-loaded languages in the game
        List<string> allLanguageNames = new();
        List<string> allLanguageCodes = new();
        GetAllLanguageNamesAndCodes(ref allLanguageNames, ref allLanguageCodes);
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

        // determine the language order by reading config
        // check if every current language is assigned a priority
        // assign unassigned languages with least priority.
        bool languagePriorityAssigned = true;
        allLanguageNames = [];
        foreach (CompiledLanguage lang in compiledLanguages)
        {
            if (!allLanguageNames.Contains(lang.NAME))
            {
                allLanguageNames.Add(lang.NAME);
            }
        }
        foreach (string langName in allLanguageNames)
        {
            if (!config.languageOrder.Contains(langName))
            {
                languagePriorityAssigned = false;
                config.languageOrder.Add(langName);
            }
        }
        // if priority is not pre-assigned, assign priority
        // languages not assigned priority originally are given largest priority value
        // multiple unassigned languages are assigned by `.txt` read-in order.
        if (!languagePriorityAssigned)
        {
            LogWarning("Missing some languages in the current language order, " +
                "unassigned languages are loaded at the end.");
        }
        // save current config into the config file
        ConfigHandler.Save<LanguageConfig>(config);

        // Load each CompiledLanguage object into the game.
        foreach (string langName in config.languageOrder)
        {
            foreach (CompiledLanguage language in compiledLanguages.FindAll(l => l.NAME == langName))
            {
                // if `base` patch is not applied for this language, do not load.
                if (!language.PATCHES_APPLIED.Contains("base"))
                {
                    LogWarning($"{language.NAME} does not have a `base` patch loaded, " +
                        $"skipping {language.NAME} entirely.");
                    continue;
                }
                RegisterTextToGame(language);
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
            int currentPatchCount = 0;
            foreach (string patchName in compiledLanguages.Find(l => l.NAME == allLanguageNames[i]).PATCHES_APPLIED)
            {
                currentPatchCount++;
                Log($"#{currentPatchCount} patch for {allLanguageNames[i]}: {patchName}");
            }
        }
    }

    /// <summary>
    /// load all terms from the `.txt` file into a `LanguagePatch` class object
    /// </summary>
    /// <param name="lang">language object being operated</param>
    private void LoadText(LanguagePatch lang)
    {
        if (!FileHandler.LoadDataAsText(lang.FILE_NAME, out string text))
        {
            LogWarning($"Could not load the {lang.FILE_NAME} file!");
            return;
        }

        int nearEmptyTermCount = 0;
        int valueEmptyTermCount = 0;
        string[] textSplit = text.Split('\n');
        foreach (string line in textSplit)
        {
            // a line with less than 5 characters (usually empty line) is definitely not a valid term
            // skip it to avoid error.
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

            // load  key, operationType, and value into corresponding lists
            if (value != string.Empty)
            {
                lang.TERM_KEYS.Add(key);
                lang.TERM_CONTENTS.Add(value);
                lang.TERM_OPERATIONS.Add(operationType);
            }
            else
            {
                LogWarning($"Skipping term {key} with empty term content.\n" +
                    $"replacing a string with empty content is not recommended.");
                valueEmptyTermCount++;
            }
        }
        Log($"Successfully loaded {lang.TERM_KEYS.Count} of {textSplit.Length} terms of {lang.NAME} for patch `{lang.PATCH_NAME}`");
        if (nearEmptyTermCount + valueEmptyTermCount > 0)
        {
            LogWarning($"Skipped {nearEmptyTermCount} near-empty terms " +
                $"and {valueEmptyTermCount} terms with empty values\n");
        }
        else
        {
            Log($"Loading process encountered no error.\n");
        }
    }

    /// <summary>
    /// load and compile all terms from the `LanguagePatch` class object 
    /// into their corresponding CompiledLanguage object
    /// </summary>
    /// <param name="lang">language object being operated</param>
    private void CompileText(LanguagePatch lang)
    {
        // counting successful and failed operations
        int successfulCount = 0;
        int operationErrorCount = 0;

        // find the corresponding CompiledLanguage object
        // if not found, create one
        if (compiledLanguages.Find(l => l.NAME == lang.NAME) == null)
        {
            compiledLanguages.Add(new CompiledLanguage(lang.NAME, lang.CODE));
        }
        int languageIndex = compiledLanguages.FindIndex(l => l.NAME == lang.NAME);

        // record this patch to the CompiledLanguage object
        compiledLanguages[languageIndex].PATCHES_APPLIED.Add(lang.PATCH_NAME);

        // compile each term patch to the CompiledLanguage object
        for (int i = 0; i < lang.TERM_KEYS.Count; i++)
        {
            string errMsg = string.Empty;
            compiledLanguages[languageIndex].UpdateTerm(lang.TERM_KEYS[i], lang.TERM_CONTENTS[i], lang.TERM_OPERATIONS[i], ref errMsg);
            if (!string.Equals(errMsg, string.Empty)) 
            {
                LogWarning(errMsg);
                operationErrorCount++;
            }
            else
            {
                successfulCount++;
            }
        }

        Log($"Successfully patched {successfulCount} of {lang.TERM_KEYS.Count} terms for {lang.NAME} from patch `{lang.PATCH_NAME}` into the game");
        if (operationErrorCount > 0)
        {
            LogWarning($"Skipped {operationErrorCount} terms with invalid operation type.\n");
        }
        else
        {
            Log($"Compiling process encountered no error.\n");
        }
    }

    /// <summary>
    /// load all the terms of `lang` into Blasphemous
    /// </summary>
    /// <param name="lang"> the CompiledLanguage object being loaded </param>
    private void RegisterTextToGame(CompiledLanguage lang)
    {
        int successfulCount = 0;
        int keyErrorCount = 0;
        // documenting whether a term isn't patched till the end due to its key being nonexistent.
        // true => this term has keyError
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
                    string termKey = lang.TERM_KEYS[i];
                    string termText = lang.TERM_PREFIXES[i] + lang.TERM_CONTENTS[i] + lang.TERM_SUFFIXES[i];
                    source.GetTermData(termKey).Languages[languageIndex] = termText;

                }
            }
        }
        for (int i = 0; i < keyErrorFlags.Count; i++)
        {
            if (keyErrorFlags[i] == true)
            {
                LogWarning($"term key {lang.TERM_KEYS[i]} not found in Blasphemous localization directory, " +
                    $"skipping this term.");
                keyErrorCount++;
            }
            else
            {
                successfulCount++;
            }
        }

        Log($"Successfully registered {successfulCount} of {lang.TERM_KEYS.Count} terms of {lang.NAME} into the game");
        if (keyErrorCount > 0)
        {
            LogWarning($"Skipped {keyErrorCount} terms with invalid keys.\n");
        }
        else
        {
            Log($"Register process encountered no error.\n");
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
/// contains the basic information of a localization patch
/// </summary>
public class LanguagePatch
{
    /// <summary>
    /// constructor of `LanguagePatch` class
    /// </summary>
    /// <param name="fullFilePath"> the full file path of the language's `.txt` file</param>
    public LanguagePatch(string fullFilePath)
    {
        if (fullFilePath == string.Empty || !File.Exists(fullFilePath))
        {
            return;
        }
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


/// <summary>
/// Contains the information of each language (one object per language, not per patch). 
/// </summary>
public class CompiledLanguage
{
    /// <summary>
    /// constructor of the CompiledLanguage class.
    /// </summary>
    /// <param name="name"> name of the language </param>
    /// <param name="code"> language code of the language </param>
    public CompiledLanguage(string name, string code)
    {
        this.NAME = name;
        this.CODE = code;
    }

    /// <summary>
    /// update a term in the CompiledLanguage object
    /// </summary>
    /// <param name="key"> key of the term being updated </param>
    /// <param name="text"> text of the term </param>
    /// <param name="operation"> operation of the term</param>
    /// <param name="errorMessage"> error message output, it's empty if no error happens </param>>
    public void UpdateTerm(string key, string text, string operation, ref string errorMessage)
    {
        errorMessage = string.Empty;

        // if the key isn't registered, register the key and all 3 types of contents
        if (!this.TERM_KEYS.Contains(key))
        {
            this.TERM_KEYS.Add(key);
            this.TERM_PREFIXES.Add(string.Empty);
            this.TERM_CONTENTS.Add(string.Empty);
            this.TERM_SUFFIXES.Add(string.Empty);
        }
        int termIndex = this.TERM_KEYS.IndexOf(key);

        if (string.Equals(operation, "Replace"))
        {
            this.TERM_CONTENTS[termIndex] = text;
        }
        else if (string.Equals(operation, "ReplaceAll"))
        {
            this.TERM_CONTENTS[termIndex] = text;
            this.TERM_PREFIXES[termIndex] = string.Empty;
            this.TERM_SUFFIXES[termIndex] = string.Empty;
        }
        else if (string.Equals(operation, "AppendAtBeginning"))
        {
            this.TERM_PREFIXES[termIndex] = text + this.TERM_PREFIXES[termIndex];
        }
        else if (string.Equals(operation, "AppendAtEnd"))
        {
            this.TERM_SUFFIXES[termIndex] = this.TERM_SUFFIXES[termIndex] + text;
        }
        else
        {
            errorMessage = $"term {key} calls for unsupported operation method {operation}, " +
                            $"skipping this term.";
        }
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
    /// All the term keys of the language terms.
    /// </summary>
    public List<string> TERM_KEYS = new();

    /// <summary>
    /// All the prefixes of term contents of the language terms.
    /// </summary>
    public List<string> TERM_PREFIXES = new();

    /// <summary>
    /// All the central term contents of the language terms.
    /// </summary>
    public List<string> TERM_CONTENTS = new();

    /// <summary>
    /// All the suffixes of term contents of the language terms.
    /// </summary>
    public List<string> TERM_SUFFIXES = new();

    /// <summary>
    /// documenting all patches applied to this language
    /// </summary>
    public List<string> PATCHES_APPLIED = new();
}