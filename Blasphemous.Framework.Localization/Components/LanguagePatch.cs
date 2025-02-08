using Blasphemous.ModdingAPI;
using Framework.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Blasphemous.Framework.Localization.Components;


/// <summary>
/// contains the basic information of a localization patch
/// </summary>
public class LanguagePatch
{
    /// <summary>
    /// The patch name of the language file.
    /// </summary>
    public string patchName;

    /// <summary>
    /// the name of the language
    /// </summary>
    public string languageName;

    /// <summary>
    /// the internal language code of the language
    /// </summary>
    public string languageCode;

    /// <summary>
    /// The mod that registered the patch.
    /// </summary>
    public string parentModId;

    /// <summary>
    /// A patch with bigger order number get patched first  
    /// among other patches registered by the same mod.
    /// </summary>
    public int patchOrder = 0;

    /// <summary>
    /// Documenting when this patch should be applied
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public PatchType patchType;

    /// <summary>
    /// Patch will be applied when this flag is true
    /// </summary>
    public string patchFlag;

    /// <summary>
    /// All the patch terms of this patch
    /// </summary>
    [JsonProperty]
    public List<PatchTerm> patchTerms = new();

    internal bool isApplied = false;

    /// <summary>
    /// Enum object describing when the patch should be applied
    /// </summary>
    public enum PatchType
    {
        /// <summary>
        /// Patched on game start
        /// </summary>
        OnInitialize,

        /// <summary>
        /// Patched when a certain flag's value is `true`
        /// </summary>
        OnFlag,

        /// <summary>
        /// Patched manually through command console
        /// </summary>
        Manually
    }


    /// <summary>
    /// The corresponding languageIndex of the CompiledLanguage object
    /// </summary>
    internal int LanguageIndex => Main.LocalizationFramework.compiledLanguages.FindIndex(l => l.languageName == languageName);


    /// <summary>
    /// Pass in the `fullText` parameter by `FileHandler.LoadDataAsText`. 
    /// </summary>
    /// <param name="patchName">see <see cref="patchName"/></param>
    /// <param name="languageName">see <see cref="languageName"/></param>
    /// <param name="languageCode">see <see cref="languageCode"/></param>
    /// <param name="patchText">Full text passed in by `FileHandler.LoadDataAsText`</param>
    /// <param name="patchOrder">see <see cref="patchOrder"/></param>
    /// <param name="patchType">see <see cref="patchType"/></param>
    /// <param name="patchFlag">see <see cref="patchFlag"/></param>
    [Obsolete($"Use JSON instead of txt files to import language patches for better compatibility. \nUse in-game command `languagepatch export [patchName]` to export your current language patches to JSON format")]
    public LanguagePatch(
        string patchName,
        string languageName,
        string languageCode,
        string patchText,
        PatchType patchType = PatchType.OnInitialize,
        string patchFlag = null,
        int patchOrder = 0)
        : this(patchName, languageName, languageCode, patchType, patchFlag, patchOrder)
    {
        LoadText(patchText);
    }

    /// <summary>
    /// Passing in the `patchTerms` directly. JSON default constructor.
    /// </summary>
    /// <param name="patchName">see <see cref="patchName"/></param>
    /// <param name="languageName">see <see cref="languageName"/></param>
    /// <param name="languageCode">see <see cref="languageCode"/></param>
    /// <param name="patchTerms">see <see cref="patchTerms"/></param>
    /// <param name="patchOrder">see <see cref="patchOrder"/></param>
    /// <param name="patchType">see <see cref="patchType"/></param>
    /// <param name="patchFlag">see <see cref="patchFlag"/></param>
    [JsonConstructor]
    public LanguagePatch(
        string patchName,
        string languageName,
        string languageCode,
        List<PatchTerm> patchTerms,
        PatchType patchType = PatchType.OnInitialize,
        string patchFlag = null,
        int patchOrder = 0)
        : this(patchName, languageName, languageCode, patchType, patchFlag, patchOrder)
    {
        this.patchTerms = patchTerms;
    }

    /// <summary>
    /// Private partial constructor, only invoked by other constructors
    /// </summary>
    private LanguagePatch(
        string patchName,
        string languageName,
        string languageCode,
        PatchType patchType = PatchType.OnInitialize,
        string patchFlag = null,
        int patchOrder = 0)
    {
        this.patchName = patchName.Trim().Replace(' ', '_');
        this.languageName = languageName;
        this.languageCode = languageCode;
        this.patchType = patchType;
        this.patchFlag = patchFlag;
        this.patchOrder = patchOrder;

        if (patchType == PatchType.OnFlag && string.IsNullOrEmpty(patchFlag))
        {
            ModLog.Error($"Missing flag for patch {patchName} that must be triggered by a specific flag!");
            return;
        }
        else if (patchType == PatchType.OnFlag && !string.IsNullOrEmpty(patchFlag))
        {
            Main.LocalizationFramework.EventHandler.OnFlagChange += OnFlagChange;
        }
    }

    /// <summary>
    /// load all terms from the raw string into <see cref="PatchTerm"/> objects
    /// </summary>
    /// <param name="rawText">Full text passed in by `FileHandler.LoadDataAsText`</param>
    public void LoadText(string rawText)
    {
        int nearEmptyTermCount = 0;
        int valueEmptyTermCount = 0;
        string[] rawTextSplit = rawText.Split('\n');

        foreach (string line in rawTextSplit)
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

            // load key, operationType, and value into corresponding lists
            if (value != string.Empty)
            {
                patchTerms.Add(new PatchTerm(key, value, operationType));
            }
            else
            {
                ModLog.Warn($"Skipping term {key} with empty term content.\n" +
                    $"replacing a string with empty content is not recommended.");
                valueEmptyTermCount++;
            }
        }
        ModLog.Info($"Successfully loaded {patchTerms.Count} of {rawTextSplit.Length} terms of {languageName} for patch `{patchName}`");
        if (nearEmptyTermCount + valueEmptyTermCount > 0)
        {
            ModLog.Warn($"Skipped {nearEmptyTermCount} near-empty terms " +
                $"and {valueEmptyTermCount} terms with empty values\n");
        }
        else
        {
            ModLog.Info($"Loading process encountered no error.\n");
        }
    }

    /// <summary>
    /// load and compile all terms from the `LanguagePatch` class object 
    /// into their corresponding `CompiledLanguage` object
    /// </summary>
    public void CompileText()
    {
        if (isApplied)
            ModLog.Warn($"Attempting to reapply patch {patchName}!");

        // counting successful and failed operations
        int successfulCount = 0;
        int operationErrorCount = 0;

        // find the corresponding CompiledLanguage object
        // if not found, create one
        if (Main.LocalizationFramework.compiledLanguages.Find(l => l.languageName == languageName) == null)
        {
            Main.LocalizationFramework.compiledLanguages.Add(new CompiledLanguage(languageName, languageCode));
        }

        // record this patch to the CompiledLanguage object
        Main.LocalizationFramework.compiledLanguages[LanguageIndex].patchesApplied.Add(patchName);

        // compile each term patch to the CompiledLanguage object
        for (int i = 0; i < patchTerms.Count; i++)
        {
            if (!Main.LocalizationFramework.compiledLanguages[LanguageIndex].TryUpdateTerm(patchTerms[i]))
            {
                operationErrorCount++;
            }
            else
            {
                successfulCount++;
            }
        }

        ModLog.Info($"Successfully patched {successfulCount} of {patchTerms.Count} terms for {languageName} from patch `{patchName}` into the game");
        if (operationErrorCount > 0)
        {
            ModLog.Warn($"Skipped {operationErrorCount} terms with invalid operation type.\n");
        }
        else
        {
            ModLog.Info($"Compiling process encountered no error.\n");
        }

        isApplied = true;
    }

    /// <summary>
    /// Apply the patch to game when the corresponding flag is set to true
    /// </summary>
    protected void OnFlagChange(string flagId)
    {
        if (flagId != patchFlag)
            return;
        if (Core.Events.GetFlag(flagId) == false)
            return;

        CompileText();
        Main.LocalizationFramework.compiledLanguages[LanguageIndex].WriteAllTermsToGame();
    }
}
