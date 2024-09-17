using Blasphemous.LocalizationPatcher.Components;
using Blasphemous.LocalizationPatcher.Events;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Files;
using I2.Loc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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
    /// the class containing config properties, loaded from `.cfg` file.
    /// </summary>
    public Config config { get; private set; }

    internal EventHandler EventHandler { get; } = new();


    protected override void OnRegisterServices(ModServiceProvider provider)
    {
#if DEBUG
        Main.LocalizationPatcher.FileHandler.LoadDataAsText("KeyDisplay_kd_base.txt", out string patch1Text);
        provider.RegisterLanguagePatch(new LanguagePatch(
            "English",
            "en",
            patch1Text));
#endif
    }


    protected override void OnAllInitialized()
    {
        // load config
        config = ConfigHandler.Load<Config>();

        // read all the term keys from base game
        foreach (LanguageSource source in LocalizationManager.Sources)
        {
            allPossibleKeys.AddRange(source.GetTermsList());
        }

        // load in all registered language `.txt` patch files
        //   and construct their corresponding LanguagePatch objects.
        // skip all disabled languages and disabled patches
        ModLog.Info("Start loading all mod language patches into the game");

        // Determine the patching order by reading config.
        // First queue all mods according to the mod order, 
        //   and for each mod, patch according to the order assigned 

        // Check if all current patches are assigned a priority in the config file
        bool patchingPriorityAssigned = true;
        // Get a list of mods that patched localization
        List<string> allPatchingModIds = new();
        foreach (LanguagePatch lang in LanguagePatchRegister.Patches)
        {
            if (!allPatchingModIds.Contains(lang.parentModId))
            {
                allPatchingModIds.Add(lang.parentModId);
            }
        }
        // if priority is not pre-assigned, assign priority
        // patches not assigned priority are given assigned to the last
        // multiple unassigned patches are assigned by register order
        // (now practically randomly ordered).
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

        // Load each langauge patch into CompiledLanguage object of corresponding language,
        //   based on priority in config.
        LanguagePatchRegister._patches.Sort(SortPatchingOrder);
        foreach (LanguagePatch patch in LanguagePatchRegister._patches)
        {
            patch.CompileText();
        }

        // Remove disabled languages in the game first
        ModLog.Info($"Removing all disabled languages of vanilla game");
        foreach (string langName in config.disabledLanguages)
        {
            try
            {
                RemoveLanguageFromGame(langName);
                ModLog.Info($"Successfully removed {langName} from game.");
            }
            catch
            {
                ModLog.Error($"failed disabling language: language named \"{langName}\" not found!");
            }
        }
        // Create CompiledLanguage objects of remaining languages before removing them.
        List<string> allLanguageNames = new();
        List<string> allLanguageCodes = new();
        GetAllLanguageNamesAndCodes(ref allLanguageNames, ref allLanguageCodes);
        foreach (string langName in allLanguageNames)
        {
            try
            {
                CompiledLanguage compiledLang = new(langName, allLanguageCodes[allLanguageNames.IndexOf(langName)]);
                compiledLang.LoadAllTermsFromGame();
                compiledLanguages.Add(compiledLang);
                RemoveLanguageFromGame(langName);
            }
            catch 
            {

                ModLog.Error($"Encountered error when initializing {langName} CompiledLanguage object!");
            }
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

        // Load each CompiledLanguage object into the game.
        foreach (string langName in config.languageOrder)
        {
            foreach (CompiledLanguage language in compiledLanguages.FindAll(l => l.languageName == langName))
            {
                language.GetAndUpdateLanguageIndex();
                language.UpdateAllTermsToGame();
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

        ConfigHandler.Save<Config>(config);
    }


    internal static void AddLanguageToGame(string langName, string langCode)
    {
        foreach (LanguageSource source in LocalizationManager.Sources)
        {
            source.AddLanguage(langName, langCode);
        }
    }

    internal static void RemoveLanguageFromGame(string langName)
    {
        foreach (LanguageSource source in LocalizationManager.Sources)
        {
            source.RemoveLanguage(langName);
        }
    }

    internal static void GetAllLanguageNamesAndCodes(ref List<string> names, ref List<string> codes)
    {
        LanguageSource source = LocalizationManager.Sources[0];
        names = source.GetLanguages();
        codes = source.GetLanguagesCode();
    }

    /// <summary>
    /// Sort patching order first by mod order, then by patchOrder
    /// </summary>
    internal int SortPatchingOrder(LanguagePatch x, LanguagePatch y)
    {
        if (x == null || y == null) return 0;
        else if (x != null  && y == null) return 1;
        else if (x == null && y != null) return -1;

        // x!= null && y != null
        // Compare mod order first
        int xModIndex = config.patchingModOrder.IndexOf(x.parentModId);
        int yModIndex = config.patchingModOrder.IndexOf(y.parentModId);
        if (xModIndex > yModIndex) return 1;
        else if (xModIndex < yModIndex) return -1;

        // xModIndex == yModIndex
        // Mod order same, compare order within same mod
        return x.patchOrder.CompareTo(y.patchOrder);
    }
}


