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
    internal LocalizationPatcher() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    internal List<CompiledLanguage> compiledLanguages = new();

    /// <summary>
    /// all terms keys in Blasphemous' localization service.
    /// </summary>
    internal List<string> allPossibleKeys = new();

    /// <summary>
    /// Loaded from `.cfg` file.
    /// </summary>
    internal Config config { get; private set; }

    internal EventHandler EventHandler { get; } = new();

    private string _debugPatchFileName = "Debug_patch_localization_key_display.json";
    private LanguagePatch _debugPatch;
    private bool _firstMainMenuEnterFlag = true;
    private string _selectedLangaugeInOptions;


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
        _selectedLangaugeInOptions = I2.Loc.LocalizationManager.CurrentLanguage;
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

        // Create CompiledLanguage objects of remaining languages
        List<string> allLanguageNames = new();
        List<string> allLanguageCodes = new();
        GetAllLanguageNamesAndCodes(ref allLanguageNames, ref allLanguageCodes);
        foreach (string langName in allLanguageNames)
        {
            try
            {
                CompiledLanguage compiledLang = new(langName, allLanguageCodes[allLanguageNames.IndexOf(langName)]);
                compiledLang.ReadAllTermsFromGame();
                compiledLanguages.Add(compiledLang);
            }
            catch (System.Exception error)
            {
                ModLog.Error($"Encountered error: `{error}` when initializing {langName} CompiledLanguage object!");
            }
        }
        // Remove all vanilla langauges
        foreach (string langName in allLanguageNames)
        {
            RemoveLanguageFromGame(langName);
        }


        // load in all registered language `.txt` patch files
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
        bool patchingPriorityAssigned = true;
        List<string> allPatchingModIds = LanguagePatchRegister.Patches.Select(x => x.parentModId).Distinct().ToList();
        foreach (string ModId in allPatchingModIds)
        {
            if (!config.patchingModOrder.Contains(ModId))
            {
                patchingPriorityAssigned = false;
                config.patchingModOrder.Add(ModId);
            }
        }
        if (!patchingPriorityAssigned)
        {
            ModLog.Warn("Current patching order of mods isn't valid, " +
                "unassigned patches are patched at the end.");
        }
        // save current config into the config file
        ConfigHandler.Save<Config>(config);

        // Load each langauge patch that are loaded on initialization into CompiledLanguage object of corresponding language based on priority in config.
        LanguagePatchRegister.SortPatchOrder();
        foreach (LanguagePatch patch in LanguagePatchRegister.Patches.Where(x => x.patchType == LanguagePatch.PatchType.OnInitialize))
        {
            patch.CompileText();
        }


        // determine the language order by reading config
        // check if every current language is assigned a priority
        // assign unassigned languages with least priority.
        bool languagePriorityAssigned = true;
        allLanguageNames = [];
        foreach (CompiledLanguage lang in compiledLanguages)
        {
            if (!allLanguageNames.Contains(lang.languageName))
            {
                allLanguageNames.Add(lang.languageName);
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
            ModLog.Warn("Missing some languages in the current language order, " +
                "unassigned languages are loaded at the end.");
        }
        // save current config into the config file
        ConfigHandler.Save<Config>(config);

        // Load each CompiledLanguage object into the game by the assigned order.
        foreach (string langName in config.languageOrder)
        {
            foreach (CompiledLanguage language in compiledLanguages.FindAll(l => l.languageName == langName))
            {
                language.WriteAllTermsToGame();
            }
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

        // Prepare to restore the selected language if it still exists
        if (Core.Localization.GetAllEnabledLanguages().Exists(x => x.Name.Equals(_selectedLangaugeInOptions)))
        {
            I2.Loc.LocalizationManager.CurrentLanguage = _selectedLangaugeInOptions;
            ModLog.Info($"Restoring selected language successful: {I2.Loc.LocalizationManager.CurrentLanguage}");
        }
        else
        {
            ModLog.Warn($"{_selectedLangaugeInOptions} not found in localization directory. It might have been deleted by this mod");
            if (!string.IsNullOrEmpty(I2.Loc.LocalizationManager.GetSupportedLanguage("English")))
            {
                ModLog.Info($"Restoring current language to English");
                _selectedLangaugeInOptions = "English";
                //UIController.instance.GetOptionsWidget().Option_AcceptGameOptions();
                //ModLog.Info($"Current language: {I2.Loc.LocalizationManager.CurrentLanguage}");
            }
            else
            {
                ModLog.Warn($"Failed to restore current language to English.");
            }
        }

        // final config save
        ConfigHandler.Save<Config>(config);
    }

    protected override void OnLevelLoaded(string oldLevel, string newLevel)
    {
        // Restore langauge option to the user-selected langauge after initialization
        if (!newLevel.Equals("MainMenu") || !_firstMainMenuEnterFlag)
            return;

        _firstMainMenuEnterFlag = false;
        I2.Loc.LocalizationManager.CurrentLanguage = _selectedLangaugeInOptions;
    }


    internal static void AddLanguageToGame(string langName, string langCode)
    {
        foreach (LanguageSource source in I2.Loc.LocalizationManager.Sources)
        {
            source.AddLanguage(langName, langCode);
        }
    }

    internal static void RemoveLanguageFromGame(string langName)
    {
        foreach (LanguageSource source in I2.Loc.LocalizationManager.Sources)
        {
            source.RemoveLanguage(langName);
        }
    }

    internal static void GetAllLanguageNamesAndCodes(ref List<string> names, ref List<string> codes)
    {
        LanguageSource source = I2.Loc.LocalizationManager.Sources[0];
        names = source.GetLanguages();
        codes = source.GetLanguagesCode();
    }
}


