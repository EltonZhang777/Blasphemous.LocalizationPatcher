using Blasphemous.CheatConsole;
using Blasphemous.LocalizationPatcher.Commands;
using Blasphemous.LocalizationPatcher.Components;
using Blasphemous.LocalizationPatcher.Events;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Persistence;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Framework.Managers;
using I2.Loc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Blasphemous.LocalizationPatcher;

internal class LocalizationPatcher : BlasMod, IGlobalPersistentMod<L10NGlobalPersistenceData>
{
    internal L10NGlobalPersistenceData globalPersistenceData = new();

    /// <summary>
    /// all terms keys in Blasphemous' localization service `I2.Loc`.
    /// </summary>
    internal List<string> allPossibleKeys = [];

    internal List<CompiledLanguage> compiledLanguages = [];
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
    internal static readonly Dictionary<string, string> vanillaRegularFontNames = new()
    {
        { "Spanish", "MajesticExtended_Pixel_Scroll" },
        { "English", "MajesticExtended_Pixel_Scroll" },
        { "French", "MajesticExtended_Pixel_Scroll" },
        { "German", "MajesticExtended_German" },
        { "Italian", "MajesticExtended_Pixel_Scroll" },
        { "Chinese", "MSJhengHei-cut" },
        { "Russian", "RussianFont_Basis33" },
        { "Japanese", "KH-Dot-Ningyouchou-16-cut" },
        { "Portuguese (Brazil)", "MajesticExtended_Pixel_Scroll" },
        { "Korean", "NeoDunggeunmo_korean_cut"}
    };
    internal static readonly Dictionary<string, string> vanillaTmpFontNames = new()
    {
        { "Spanish", "MajesticExtended_FullLatin" },
        { "English", "MajesticExtended_FullLatin" },
        { "French", "MajesticExtended_FullLatin" },
        { "German", "MajesticExtended_GermanPro" },
        { "Italian", "MajesticExtended_FullLatin" },
        { "Chinese", "MSJhengHei-cutPro" },
        { "Russian", "RussianFont_Basis33_exported" },
        { "Japanese", "KH-Dot-Ningyouchou-16-cutPro" },
        { "Portuguese (Brazil)", "MajesticExtended_FullLatin" },
        { "Korean", "NeoDunggeunmo_korean_cutPro"}
    };
    private readonly string _debugPatchFileName = "Debug_patch_localization_key_display.json";
    private LanguagePatch _debugPatch;
    private bool _firstMainMenuEnterFlag = true;
    private string _selectedLangaugeInOptions;

    /// <summary>
    /// Loaded from `.cfg` file.
    /// </summary>
    internal Config config { get; private set; }

    internal EventHandler EventHandler { get; } = new();

    internal SystemFontManager SystemFontManager { get; private set; } = new();

    internal LocalizationPatcher() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    protected override void OnInitialize()
    {
        // load config
        config = ConfigHandler.Load<Config>();

        // The debug patch file doubles as the term-key index for all languages, which speeds up
        // key lookup. It is not required for the mod to function: if missing, all term keys are
        // enumerated dynamically from I2.Loc sources in OnAllInitialized instead.
        string debugPatchFullPath = Path.Combine(Path.Combine(Path.Combine(FileHandler.ModdingFolder, "data"), Name), _debugPatchFileName);
        if (File.Exists(debugPatchFullPath))
        {
            FileHandler.LoadDataAsJson<LanguagePatch>(_debugPatchFileName, out _debugPatch);
            allPossibleKeys = _debugPatch.patchTerms.Select(x => x.termKey).Distinct().ToList();
            ModLog.Info($"Loaded {allPossibleKeys.Count} term keys from debug patch file.");
        }
        else
        {
            ModLog.Warn($"Debug patch file `{_debugPatchFileName}` not found, falling back to dynamic key enumeration.");
        }
    }

    protected override void OnRegisterServices(ModServiceProvider provider)
    {
        // register commands
        List<ModCommand> commands =
            [
            new LanguagePatchCommand(),
            new ModFontCommand(),
            ];
        foreach (ModCommand command in commands)
        {
            provider.RegisterCommand(command);
        }

        // register all patches in the `auto-load language patches` folder under data path
        string autoLoadPatchesPath = Path.Combine(FileHandler.GetDataPath(), "auto-load language patches");
        if (Directory.Exists(autoLoadPatchesPath))
        {
            // JSON patches are loaded as-is
            foreach (string filePath in Directory.GetFiles(autoLoadPatchesPath, "*.json"))
            {
                try
                {
                    string relativePath = Path.Combine("auto-load language patches", Path.GetFileName(filePath));
                    FileHandler.LoadDataAsJson<LanguagePatch>(relativePath, out LanguagePatch autoPatch);
                    provider.RegisterLanguagePatch(autoPatch);
                }
                catch (System.Exception error)
                {
                    ModLog.Error($"Failed to auto-load language patch `{Path.GetFileName(filePath)}`: {error.Message}. \nSkipping this file.");
                }
            }

            // txt patches follow the `[Language Name]_[Language Code]_[Patch Name].txt` naming
            // convention and are always applied OnInitialize
            foreach (string filePath in Directory.GetFiles(autoLoadPatchesPath, "*.txt"))
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string[] nameParts = fileName.Split('_');
                if (nameParts.Length < 3)
                {
                    ModLog.Warn($"Skipping txt patch `{Path.GetFileName(filePath)}`: filename must follow `[Language Name]_[Language Code]_[Patch Name].txt` format.");
                    continue;
                }

                string languageName = nameParts[0];
                string languageCode = nameParts[1];
                string patchName = string.Join("_", nameParts.Skip(2).ToArray());

                LanguagePatch txtPatch = new(patchName, languageName, languageCode, [], LanguagePatch.PatchType.OnInitialize);
                txtPatch.LoadText(File.ReadAllText(filePath));
                provider.RegisterLanguagePatch(txtPatch);
                ModLog.Info($"Auto-loaded txt language patch `{txtPatch.patchName}` for `{languageName}`.");
            }
        }

#if DEBUG
        // load debug test patch
        if (_debugPatch != null)
        {
            provider.RegisterLanguagePatch(_debugPatch);
        }

        // load debug font
        List<string> debugFontNames =
            [
            //"vonwaonbitmap-12px",
            "vonwaonbitmap-16px",
            //"ms-yahei",
            ];
        debugFontNames.ForEach(x => provider.RegisterModFont(new ModFont(FileHandler, $"{x}.json")));
#endif
    }

    protected override void OnAllInitialized()
    {
        ModLog.Info($"Loaded {LanguagePatchRegister.Total} language patches from all mod registers");

        // If the debug patch file was missing during OnInitialize, enumerate all term keys
        // from I2.Loc sources so that key lookups still work. Must run before any
        // CompiledLanguage object is constructed (they share the allPossibleKeys list).
        if (allPossibleKeys.Count == 0)
        {
            ModLog.Info("Debug patch file missing, enumerating term keys from I2.Loc sources...");
            List<string> enumeratedKeys = [];
            foreach (LanguageSource source in I2LocManager.Sources)
            {
                enumeratedKeys.AddRange(source.GetTermsList());
            }
            allPossibleKeys.AddRange(enumeratedKeys.Distinct());
            ModLog.Info($"Enumerated {allPossibleKeys.Count} term keys from I2.Loc sources.");
        }

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
        List<string> allLanguageNames = [];
        List<string> allLanguageCodes = [];
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

        // remove disabled patches from the register so they are never compiled,
        // triggered by flags, or applied to the game
        foreach (string disabledPatchName in config.disabledPatches)
        {
            LanguagePatch disabledPatch = LanguagePatchRegister.AtName(disabledPatchName);
            if (disabledPatch == null)
                continue;

            disabledPatch.UnregisterFlagEvent();
            LanguagePatchRegister.RemovePatch(disabledPatch);
            ModLog.Info($"Skipped disabled patch `{disabledPatch.patchName}`.");
        }

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
            CompiledLanguage compiledLang = compiledLanguages.Find(l => l.languageName == langName);
            if (compiledLang == null)
            {
                ModLog.Warn($"Language `{langName}` from `languageOrder` config not found among compiled languages, skipping.");
                continue;
            }
            // force write all if needRemoveVanillaLanguages is true
            compiledLang.WriteAllTermsToGame(needRemoveVanillaLanguages);
        }

#if DEBUG
        // display all current languages into log
        GetAllLanguageNamesAndCodes(ref allLanguageNames, ref allLanguageCodes);
        StringBuilder sb = new();
        sb.AppendLine($"Final summary of all loaded languages:");
        int numCurrentLanguages = allLanguageNames.Count;
        for (int i = 0; i < numCurrentLanguages; i++)
        {
            sb.AppendLine($"  Language #{i + 1} :");
            sb.AppendLine($"    language name: {allLanguageNames[i]}");
            sb.AppendLine($"    language code: {allLanguageCodes[i]}");
            int currentPatchCount = 0;
            CompiledLanguage compiledLang = compiledLanguages.Find(l => l.languageName == allLanguageNames[i]);
            if (compiledLang == null)
                continue;
            foreach (string patchName in compiledLang.patchesApplied)
            {
                currentPatchCount++;
                sb.AppendLine($"#{currentPatchCount} patch for {allLanguageNames[i]}: {patchName}");
            }
        }
        ModLogExtensions.WarnIfDebugBuild(sb.ToString());
#endif

        // Hook all ModFont objects to CompiledLanguage objects
        foreach (ModFont modFont in ModFontRegister.ModFonts)
        {
            modFont.AttachFontToLanguages();
        }

        // final config save
        ConfigHandler.Save<Config>(config);
    }

    protected override void OnLevelLoaded(string oldLevel, string newLevel)
    {
        if (newLevel.Equals("MainMenu") && _firstMainMenuEnterFlag)
        {
            _firstMainMenuEnterFlag = false;
            OnLoadMainMenuFirstTime();
        }

        // entering game level from main menu
        if (!newLevel.Equals("MainMenu") && oldLevel.Equals("MainMenu"))
        {
            OnEnterSaveFromMainMenu();
        }
    }

    /// <summary>
    /// Executes when the game enters the main menu for the first time. 
    /// Useful for treating processes requiring saveData because it is read after <c>OnAllInitialized</c>.
    /// </summary>
    private void OnLoadMainMenuFirstTime()
    {
        // Determine language chosen on startup
        // read save data first, use save data settings if the language is loaded
        if (string.IsNullOrEmpty(globalPersistenceData.languageOnStartup)
            || !Core.Localization.GetAllEnabledLanguages().Exists(x => x.Name.Equals(globalPersistenceData.languageOnStartup)))
        {
            // if not set or does not exist, use the stored language in game settings
            globalPersistenceData.languageOnStartup = _selectedLangaugeInOptions;

            if (string.IsNullOrEmpty(globalPersistenceData.languageOnStartup)
            || !Core.Localization.GetAllEnabledLanguages().Exists(x => x.Name.Equals(globalPersistenceData.languageOnStartup)))
            {
                // if language in settings does not exist, default to English
                globalPersistenceData.languageOnStartup = "English";
            }
            ModLog.Info($"No language on startup set in save data, using language: {globalPersistenceData.languageOnStartup}");
        }
        else
        {
            ModLog.Info($"Using language on startup from save data: {globalPersistenceData.languageOnStartup}");
        }

        // Restore langauge option to the user-selected langauge after entering main menu for the first time
        I2LocManager.CurrentLanguage = globalPersistenceData.languageOnStartup;

        // Restore all saved fonts from persistence data
        ModLog.Info("Restoring saved fonts...");
        foreach (KeyValuePair<string, List<string>> entry in globalPersistenceData.languageCodeToAppliedFonts.ToList())
        {
            ModLogExtensions.WarnIfDebugBuild($"Restoring saved fonts for language code `{entry.Key}`...");
            string languageCode = entry.Key;
            foreach (string fontName in entry.Value.ToList())
            {
                CompiledLanguage compiledLang = compiledLanguages.FirstOrDefault(x => x.languageCode == languageCode);
                if (compiledLang == null)
                {
                    ModLog.Warn($"Language code `{languageCode}` not found when restoring saved font `{fontName}`.");
                    continue;
                }

                // Try restoring as a mod font
                ModFont modFont = ModFontRegister.ModFonts.FirstOrDefault(x => x.info.fontName == fontName);
                if (modFont != null)
                {
                    ModLog.Info($"Restoring saved mod font `{fontName}` to `{compiledLang.languageName}`.");
                    compiledLang.ApplyFontToGame(modFont);
                    continue;
                }

                // Try restoring as a system font
                if (SystemFontManager.HasSystemFont(fontName))
                {
                    ModLog.Info($"Restoring saved system font `{fontName}` to `{compiledLang.languageName}`.");
                    SystemFontManager.TryApplySystemFont(fontName, compiledLang.languageName);
                    continue;
                }

                ModLog.Warn($"Saved font `{fontName}` not found for language `{compiledLang.languageName}`.");
            }
        }
    }

    private void OnEnterSaveFromMainMenu()
    {
        // check every flag-triggered patch and apply the patch if the flag is set to true
        foreach (LanguagePatch patch in LanguagePatchRegister.Patches.Where(x => x.patchType == LanguagePatch.PatchType.OnFlag))
        {
            // normalize the flag id so save-entry checks match the runtime
            // (Harmony SetFlag) path, where flags arrive already normalized.
            string formattedFlag = LanguagePatch.FormatFlag(patch.patchFlag);
            ModLogExtensions.WarnIfDebugBuild($"Checking flag-triggered patch `{patch.patchName}` with flag `{formattedFlag}`: {Core.Events.GetFlag(formattedFlag)}");
            if (Core.Events.GetFlag(formattedFlag))
            {
                ModLog.Info($"Applying flag-triggered patch `{patch.patchName}` with flag `{formattedFlag}`.");
                patch.OnFlagChange(formattedFlag);
            }
            else
            {
                ModLog.Info($"Deactivating flag-triggered patch `{patch.patchName}` with flag `{formattedFlag}`.");
                patch.OnFlagChange(formattedFlag);
            }
        }
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

    public L10NGlobalPersistenceData SaveGlobal()
    {
        // store current selected langauge to saveData for startup next time
        globalPersistenceData.languageOnStartup = I2LocManager.CurrentLanguage;

        return globalPersistenceData;
    }

    public void LoadGlobal(L10NGlobalPersistenceData data)
    {
        globalPersistenceData = data;
    }
}

