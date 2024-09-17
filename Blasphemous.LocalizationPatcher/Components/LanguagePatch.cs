using Blasphemous.ModdingAPI.Files;
using Blasphemous.ModdingAPI;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq.Expressions;
using Framework.Managers;

namespace Blasphemous.LocalizationPatcher.Components;


/// <summary>
/// contains the basic information of a localization patch
/// </summary>
public class LanguagePatch
{
    /// <summary>
    /// the name of the language
    /// </summary>
    public string languageName;

    /// <summary>
    /// the internal language code of the language
    /// </summary>
    public string languageCode;

    /// <summary>
    /// The patch name of the language file.
    /// Patch name for language initialization files (containing all strings) is "base".
    /// </summary>
    public string patchName;

    /// <summary>
    /// The mod that registered the patch.
    /// </summary>
    public string parentModId;

    /// <summary>
    /// A patch with smaller order number get patched first  
    /// among other patches registered by the same mod.
    /// </summary>
    public int patchOrder = int.MinValue;

    /// <summary>
    /// Documenting when this patch should be applied
    /// </summary>
    public PatchType patchType;

    /// <summary>
    /// Patch will be applied when this flag is true
    /// </summary>
    public string patchFlag;

    /// <summary>
    /// All the term keys of the language terms.
    /// </summary>
    public List<string> termKeys = new();

    /// <summary>
    /// All the term contents of the language terms.
    /// </summary>
    public List<string> termContents = new();

    /// <summary>
    /// All the operations to be executed to the language terms.
    /// </summary>
    public List<string> termOperations = new();

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
        OnFlag
    }


    /// <summary>
    /// Pass in the `fullText` parameter by `FileHandler.LoadDataAsText`. 
    /// </summary>
    /// <param name="langName">see <see cref="languageName"/></param>
    /// <param name="langCode">see <see cref="languageCode"/></param>
    /// <param name="fullText">Full text passed in by `FileHandler.LoadDataAsText`</param>
    /// <param name="order">see <see cref="patchOrder"/></param>
    /// <param name="type">see <see cref="patchType"/></param>
    /// <param name="flag">see <see cref="patchFlag"/></param>
    public LanguagePatch(
        string langName,
        string langCode,
        string fullText,
        int order = int.MinValue,
        PatchType type = PatchType.OnInitialize,
        string flag = null)
    {
        languageName = langName;
        languageCode = langCode;
        patchOrder = order;
        patchType = type;
        patchFlag = flag;
        if (type == PatchType.OnFlag && (flag == null || flag == string.Empty))
        {
            string errMsg = $"Missing flag for patch {patchName} that must be triggered by a specific flag!";
            ModLog.Error(errMsg);
            throw new System.ArgumentNullException(errMsg);
        }
        else if (type == PatchType.OnFlag && flag != null && flag != string.Empty)
        {
            Main.LocalizationPatcher.EventHandler.OnFlagChange += OnFlagSetToTrue;
        }

        LoadText(fullText);
    }

    /// <summary>
    /// load all terms from the raw string into term keys, contents, and operations
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

            // load  key, operationType, and value into corresponding lists
            if (value != string.Empty)
            {
                termKeys.Add(key);
                termContents.Add(value);
                termOperations.Add(operationType);
            }
            else
            {
                ModLog.Warn($"Skipping term {key} with empty term content.\n" +
                    $"replacing a string with empty content is not recommended.");
                valueEmptyTermCount++;
            }
        }
        ModLog.Info($"Successfully loaded {termKeys.Count} of {rawTextSplit.Length} terms of {languageName} for patch `{patchName}`");
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
    /// into their corresponding CompiledLanguage object
    /// </summary>
    public void CompileText()
    {
        // counting successful and failed operations
        int successfulCount = 0;
        int operationErrorCount = 0;

        // find the corresponding CompiledLanguage object
        // if not found, create one
        if (Main.LocalizationPatcher.compiledLanguages.Find(l => l.languageName == languageName) == null)
        {
            Main.LocalizationPatcher.compiledLanguages.Add(new CompiledLanguage(languageName, languageCode));
        }
        int languageIndex = Main.LocalizationPatcher.compiledLanguages.FindIndex(l => l.languageName == languageName);

        // record this patch to the CompiledLanguage object
        Main.LocalizationPatcher.compiledLanguages[languageIndex].patchesApplied.Add(patchName);

        // compile each term patch to the CompiledLanguage object
        for (int i = 0; i < termKeys.Count; i++)
        {
            if (!Main.LocalizationPatcher.compiledLanguages[languageIndex].TryUpdateTerm(
                    termKeys[i],
                    termContents[i],
                    termOperations[i]))
            {
                operationErrorCount++;
            }
            else
            {
                successfulCount++;
            }
        }

        ModLog.Info($"Successfully patched {successfulCount} of {termKeys.Count} terms for {languageName} from patch `{patchName}` into the game");
        if (operationErrorCount > 0)
        {
            ModLog.Warn($"Skipped {operationErrorCount} terms with invalid operation type.\n");
        }
        else
        {
            ModLog.Info($"Compiling process encountered no error.\n");
        }
    }

    /// <summary>
    /// Apply the patch to game when the corresponding flag is set to true
    /// </summary>
    protected void OnFlagSetToTrue(string flagId)
    {
        if (flagId != patchFlag)  return;
        if (Core.Events.GetFlag(flagId) == false) return;
        CompileText();
    }
}
