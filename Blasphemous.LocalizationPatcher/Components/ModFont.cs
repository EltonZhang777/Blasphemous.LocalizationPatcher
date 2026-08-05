using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Files;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace Blasphemous.LocalizationPatcher.Components;

/// <summary>
/// Containing modded font information and assets
/// </summary>
public class ModFont
{
    /// <summary>
    /// Containing meta-information of the mod font
    /// </summary>
    public ModFontInfo info;

    /// <summary>
    /// The mod that registered the patch.
    /// </summary>
    public string parentModId;

    /// <summary>
    /// TextMeshPro font asset of the mod font
    /// </summary>
    public TMP_FontAsset tmpFont;

    /// <summary>
    /// Regular TTF font asset used for the mod font
    /// </summary>
    public Font ttfFont;

    public string TmpAssetName => info.fontName + "_tmp";
    public string TtfAssetName => info.fontName + "_ttf";

    /// <summary>
    /// Standard constructor of ModFont, automatically loads related assets.
    /// </summary>
    public ModFont(ModFontInfo info)
    {
        this.info = info;

        // load all assets, and extract TMP_FontAsset and Font from the AssetBundle
        AssetBundle ab;
        if (!Main.LocalizationPatcher.FileHandler.LoadDataAsAssetBundle(info.fileName, out ab))
        {
            string errMsg = $"AssetBundle `{info.fileName}` not found!";
            ModLog.Error(errMsg);
            throw new System.ArgumentException(errMsg);
        }

        ModLogExtensions.WarnIfDebugBuild($"Loading assetBundle!");
#if DEBUG
        StringBuilder sb = new();
        sb.AppendLine($"All assets in AssetBundle {ab.name}: ");
        foreach (UObject asset in ab.LoadAllAssets())
        {
            sb.AppendLine($"  asset name: `{asset.name}`; asset type: `{asset.GetType()}`");
        }
        sb.AppendLine();
        ModLogExtensions.WarnIfDebugBuild(sb.ToString());
#endif

        // load ttf assets
        ttfFont = ab.LoadAllAssets<Font>().FirstOrDefault();
        ttfFont.name = TtfAssetName;

        //// load tmp asset
        //tmpFont = ab.LoadAllAssets<TMP_FontAsset>().FirstOrDefault();
        //tmpFont.name = TmpAssetName;
    }

    /// <summary>
    /// Constructor that reads ModFontInfo by loading a JSON file.
    /// </summary>
    public ModFont(
        FileHandler fileHandler,
        string InfoJsonFileLocation)
        : this(fileHandler.LoadDataAsJson<ModFontInfo>(InfoJsonFileLocation))
    { }

    /// <summary>
    /// Attach this font to all its supported languages
    /// </summary>
    public void AttachFontToLanguages()
    {
        List<string> removedLanguages = [];
        foreach (string langName in info.supportedLanguages)
        {
            if (!Main.LocalizationPatcher.compiledLanguages.Exists(x => x.languageName == langName))
            {
                // if the language does not exist, mark it for removal
                removedLanguages.Add(langName);
            }
            else
            {
                // attach the font to the language
                Main.LocalizationPatcher.compiledLanguages
                    .First(x => x.languageName == langName)
                    .modFonts.Add(this);
            }
        }

        // batch remove languages that are not in the compiled languages list
        foreach (string langName in removedLanguages)
        {
            info.supportedLanguages.Remove(langName);
        }
    }
}
