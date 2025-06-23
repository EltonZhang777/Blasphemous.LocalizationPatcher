using Blasphemous.ModdingAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.LocalizationPatcher.Components;

/// <summary>
/// A single term that records modification to localization string
/// </summary>
public class PatchTerm
{
    /// <summary>
    /// Key of the term in the game's localization directory: <see cref="I2LocManager"/>
    /// </summary>
    [JsonProperty]
    public string termKey;

    /// <summary>
    /// Actual text content of the term modification
    /// </summary>
    [JsonProperty]
    public string termContent;

    /// <summary>
    /// Operation to be executed to the language term
    /// </summary>
    [JsonProperty]
    [JsonConverter(typeof(StringEnumConverter))]
    public TermOperation termOperation;

    /// <summary>
    /// Type of operation to be executed to the language term
    /// </summary>
    public enum TermOperation
    {
        /// <summary>
        /// Replace the original text with your custom text. Will not affect prefixes and suffixes added by this mod
        /// </summary>
        Replace,

        /// <summary>
        /// Replace the original text and all of its custom prefixes and suffixes with your custom text.
        /// </summary>
        ReplaceAll,

        /// <summary>
        /// Add your custom text as a prefix, attached to the beginning of the original text.
        /// </summary>
        Prefix,

        /// <summary>
        /// Add your custom text as a suffix, attached to the end of the original text.
        /// </summary>
        Suffix
    }

    private static readonly Dictionary<TermOperation, List<string>> _termOperationParseDict = new()
    {
        { TermOperation.Replace, ["Replace"] },
        { TermOperation.ReplaceAll, ["ReplaceAll"] },
        { TermOperation.Prefix, ["Prefix", "AppendAtBeginning"] },
        { TermOperation.Suffix, ["Suffix", "AppendAtEnd"] }
    };

    /// <summary>
    /// Constructor of <see cref="PatchTerm"/>
    /// </summary>
    /// <param name="termKey">see <see cref="termKey"/></param>
    /// <param name="termContent">see <see cref="termContent"/></param>
    /// <param name="termOperation">see <see cref="termOperation"/></param>
    [JsonConstructor]
    public PatchTerm(
        string termKey,
        string termContent,
        TermOperation termOperation)
    {
        this.termKey = termKey;
        this.termContent = termContent;
        this.termOperation = termOperation;
    }

    /// <summary>
    /// Constructor of <see cref="PatchTerm"/>
    /// </summary>
    /// <param name="termKey">see <see cref="termKey"/></param>
    /// <param name="termContent">see <see cref="termContent"/></param>
    /// <param name="termOperation">see <see cref="termOperation"/></param>
    public PatchTerm(
        string termKey,
        string termContent,
        string termOperation)
    {
        this.termKey = termKey;
        this.termContent = termContent;
        try
        {
            this.termOperation = ParseToTermOperation(termOperation);
        }
        catch
        {
            ModLog.Error($"Invalid term operation `{termOperation}` for term `{termKey}`!");
            this.termKey = "";
            this.termContent = "";
            this.termOperation = TermOperation.Prefix;
        }
    }

    /// <summary>
    /// Parse string to <see cref="TermOperation"/>
    /// </summary>
    public static TermOperation ParseToTermOperation(string input, bool ignoreCase = false)
    {
        foreach (var kvp in _termOperationParseDict)
        {
            if (!ignoreCase)
            {
                if (kvp.Value.Contains(input))
                    return kvp.Key;
            }
            else
            {
                if (kvp.Value.Select(x => x.ToLower()).Contains(input.ToLower()))
                    return kvp.Key;
            }
        }

        string errorMessage = $"Failed to parse termOperation to enum! Defaulting to `Replace`";
        ModLog.Error(errorMessage);
        throw new ArgumentException(errorMessage);
    }
}
