using Blasphemous.CheatConsole;
using Blasphemous.LocalizationPatcher.Commands;
using Blasphemous.LocalizationPatcher.Components;
using Blasphemous.LocalizationPatcher.Events;
using Blasphemous.ModdingAPI;
using Framework.Managers;
using I2.Loc;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Blasphemous.LocalizationPatcher;


internal class LocalizationPatcher : BlasMod
{
    /// <summary>
    /// all terms keys in Blasphemous' localization service `I2.Loc`.
    /// </summary>
    internal List<string> allPossibleKeys = new();

    internal List<CompiledLanguage> compiledLanguages = new();
    internal static readonly List<string> vanillaLanguageNames =
        [
        "Spanish",
        "English",
        "French",
        "German",
        "Italian",
        "Chinese",
        "Russian",
        "Japanese",
        "Portuguese (Brazil)",
        "Korean"
        ];

    private readonly string _debugPatchFileName = "Debug_patch_localization_key_display.json";
    private LanguagePatch _debugPatch;
    private bool _firstMainMenuEnterFlag = true;
    private string _selectedLangaugeInOptions;

    /// <summary>
    /// Loaded from `.cfg` file.
    /// </summary>
    internal Config config { get; private set; }

    internal EventHandler EventHandler { get; } = new();

    internal LocalizationPatcher() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    protected override void OnInitialize()
    {
        // load config
        config = ConfigHandler.Load<Config>();

        if (!File.Exists(Path.GetFullPath(FileHandler.ModdingFolder + "data/" + base.Name + @"/" + _debugPatchFileName)))
        {
            string errorMessage = $"debug patch not found!";
            ModLog.Error(errorMessage);
            throw new FileNotFoundException(errorMessage);
        }

        FileHandler.LoadDataAsJson<LanguagePatch>(_debugPatchFileName, out _debugPatch);
        allPossibleKeys = _debugPatch.patchTerms.Select(x => x.termKey).Distinct().ToList();
    }

    protected override void OnRegisterServices(ModServiceProvider provider)
    {
        // register commands
        List<ModCommand> commands =
            [
            new LanguagePatchCommand()
            ];
        foreach (ModCommand command in commands)
        {
            provider.RegisterCommand(command);
        }

#if DEBUG
        // load the debug test patch
        provider.RegisterLanguagePatch(_debugPatch);

        // text-based import test
        provider.RegisterLanguagePatch(new LanguagePatch(
            "Debug_patch_addition_patch",
            "KeyDisplay",
            "kd",
            "UI_Map/LABEL_MENU_LANGUAGENAME -> AppendAtBeginning : test_",
            LanguagePatch.PatchType.Manually));

        // (de)serialization test
        /*
        ModLog.Info($"Start serializing LanguagePatch");
        File.WriteAllText(
            FileHandler.ContentFolder + @"test_patch.json",
            JsonConvert.SerializeObject(debugPatch, Formatting.Indented));

        ModLog.Info($"Start deserializing to LanguagePatch");
        LanguagePatch deserializedPatch = JsonConvert.DeserializeObject<LanguagePatch>(
            JsonConvert.SerializeObject(debugPatch, Formatting.Indented));
        string reserializedJson = JsonConvert.SerializeObject(deserializedPatch, Formatting.Indented);
        File.WriteAllText(
            FileHandler.ContentFolder + @"reserialized_patch.json",
            reserializedJson);
        ModLog.Info($"json equal after reserialization? {reserializedJson.Equals(JsonConvert.SerializeObject(debugPatch, Formatting.Indented))}");
        */
#endif
    }


    protected override void OnAllInitialized()
    {
        ModLog.Info($"Loaded {LanguagePatchRegister.Total} language patches from all mod registers");

        // store the language selected by the player in settings, so that it can be restored after patching completes
        _selectedLangaugeInOptions = I2LocManager.CurrentLanguage;
        ModLog.Info($"Stored current language selection: {_selectedLangaugeInOptions}");

        // Remove disabled languages in the game first
        ModLog.Info($"Removing all disabled languages of vanilla game:");
        int removedLanguageCount = 0;
        foreach (string langName in config.disabledLanguages)
        {
            try
            {
                RemoveLanguageFromGame(langName);
                ModLog.Info($"  Successfully removed {langName} from game.");
                removedLanguageCount++;
            }
            catch
            {
                ModLog.Error($"  Failed disabling language: language named \"{langName}\" not found!");
            }
        }
        ModLog.Info($"Successfully removed {removedLanguageCount} languages from game.");

        // Create CompiledLanguage objects of remaining vanilla languages
        List<string> allLanguageNames = new();
        List<string> allLanguageCodes = new();
        GetAllLanguageNamesAndCodes(ref allLanguageNames, ref allLanguageCodes);
        foreach (string langName in allLanguageNames)
        {
            try
            {
                CompiledLanguage compiledLang = RegisterCompiledLanguageObject(langName, allLanguageCodes[allLanguageNames.IndexOf(langName)]);
                compiledLang?.UpdateLanguageIndex();
                compiledLang?.ReadAllTermsFromGame();
            }
            catch (System.Exception error)
            {
                ModLog.Error($"Encountered error: `{error}` when initializing {langName} CompiledLanguage object!");
            }
        }

        // load all registered language patch files
        //   and construct their corresponding LanguagePatch objects.
        // skip all disabled languages and disabled patches
        ModLog.Info("Start writing all mod language patches into the game");

        // Determine the patching order by reading config.
        // First queue all mods according to the mod order, 
        //   and for each mod, patch according to the order assigned 
        // Check if all current patches are assigned a priority in the config file
        // If priority is not pre-assigned, assign priority
        // Patches not assigned priority are given assigned to the last
        // Multiple unassigned patches are assigned by register order
        //   (now practically randomly ordered).

        // validate and resolve mod patching order
        Main.ValidateAndResolveSortingOrder(ref config.patchingModOrder, LanguagePatchRegister.Patches.Select(x => x.parentModId).Distinct().ToList());
        // save current config into the config file
        ConfigHandler.Save<Config>(config);

        // Load each langauge patch that are loaded on initialization into CompiledLanguage object of corresponding language based on priority in config.
        LanguagePatchRegister.SortPatchOrder();
        foreach (LanguagePatch patch in LanguagePatchRegister.Patches.Where(x => x.patchType == LanguagePatch.PatchType.OnInitialize))
        {
            patch.CompileText();
        }

        // validate and resolve language priority order
        Main.ValidateAndResolveSortingOrder(ref config.languageOrder, compiledLanguages.Select(x => x.languageName).ToList());
        // arrange langauges in I2.Loc sources according to the order in config
        // If languageOrder does not start in the same order with remaining vanilla languages, remove all remaining vanilla langauges and add them back later, in the order specified in config.
        GetAllLanguageNamesAndCodes(ref allLanguageNames, ref allLanguageCodes);
        bool needRemoveVanillaLanguages = false;
        for (int i = 0; i < allLanguageNames.Count; i++)
        {
            if (i >= config.languageOrder.Count)
            {
                needRemoveVanillaLanguages = true;
                break;
            }
            if (!config.languageOrder[i].Equals(allLanguageNames[i]))
            {
                needRemoveVanillaLanguages = true;
                break;
            }
        }
        if (needRemoveVanillaLanguages)
        {
            foreach (string langName in allLanguageNames)
            {
                RemoveLanguageFromGame(langName);
            }
        }

        // save current config into the config file
        ConfigHandler.Save<Config>(config);

        // Write all modified terms in CompiledLanguage objects into the game by the assigned order.
        foreach (string langName in config.languageOrder)
        {
            // force write all if needRemoveVanillaLanguages is true
            compiledLanguages.Find(l => l.languageName == langName).WriteAllTermsToGame(needRemoveVanillaLanguages);
        }

#if DEBUG
        // display all current languages into log
        GetAllLanguageNamesAndCodes(ref allLanguageNames, ref allLanguageCodes);
        ModLog.Info($"Final summary of all loaded languages:");
        int numCurrentLanguages = allLanguageNames.Count;
        for (int i = 0; i < numCurrentLanguages; i++)
        {
            ModLog.Info($"\nlanguage #{i + 1} : \n" +
                $"language name: {allLanguageNames[i]}\n" +
                $"language code: {allLanguageCodes[i]}");
            int currentPatchCount = 0;
            foreach (string patchName in compiledLanguages.Find(l => l.languageName == allLanguageNames[i]).patchesApplied)
            {
                currentPatchCount++;
                ModLog.Info($"#{currentPatchCount} patch for {allLanguageNames[i]}: {patchName}");
            }
        }
#endif

        // Determine language chosen on startup
        // read config first, use config settings if the language is loaded
        if (string.IsNullOrEmpty(config.languageOnStartup)
            || !Core.Localization.GetAllEnabledLanguages().Exists(x => x.Name.Equals(config.languageOnStartup)))
        {
            // if not set or does not exist, use the stored language in game settings
            config.languageOnStartup = _selectedLangaugeInOptions;

            if (string.IsNullOrEmpty(config.languageOnStartup)
            || !Core.Localization.GetAllEnabledLanguages().Exists(x => x.Name.Equals(config.languageOnStartup)))
            {
                // if language in settings does not exist, default to English
                config.languageOnStartup = "English";
            }
            ModLog.Info($"No language on startup set in config, using language: {config.languageOnStartup}");
        }
        else
        {
            ModLog.Info($"Using language on startup from config: {config.languageOnStartup}");
        }
        // actual language setting is done when loading main menu

        // final config save
        ConfigHandler.Save<Config>(config);
    }

    protected override void OnLevelLoaded(string oldLevel, string newLevel)
    {
        // Restore langauge option to the user-selected langauge after entering main menu for the first time
        if (newLevel.Equals("MainMenu") && _firstMainMenuEnterFlag)
        {
            _firstMainMenuEnterFlag = false;
            I2LocManager.CurrentLanguage = config.languageOnStartup;
        }

    }

    protected override void OnDispose()
    {
        // store current selected langauge to config for startup next time
        config.languageOnStartup = I2LocManager.CurrentLanguage;
        ConfigHandler.Save<Config>(config);
    }

    internal CompiledLanguage RegisterCompiledLanguageObject(string langName, string langCode)
    {
        if (compiledLanguages.Exists(x => x.languageName == langName)
            || compiledLanguages.Exists(x => x.languageCode == langCode))
        {
            ModLog.Warn($"Aborted attempt to register already-existing CompiledLanguage object of `{langName}`");
            return null;
        }

        CompiledLanguage compiledLang = new(langName, langCode);
        compiledLanguages.Add(compiledLang);
        return compiledLang;
    }

    internal static void AddLanguageToGame(string langName, string langCode)
    {
        foreach (LanguageSource source in I2LocManager.Sources)
        {
            source.AddLanguage(langName, langCode);
        }
    }

    internal static void RemoveLanguageFromGame(string langName)
    {
        foreach (LanguageSource source in I2LocManager.Sources)
        {
            source.RemoveLanguage(langName);
        }
    }

    internal static void GetAllLanguageNamesAndCodes(ref List<string> names, ref List<string> codes)
    {
        LanguageSource source = I2LocManager.Sources[0];
        names = source.GetLanguages();
        codes = source.GetLanguagesCode();
    }

    internal static bool IsVanillaLanguage(string langName)
    {
        return vanillaLanguageNames.Contains(langName);
    }

}


