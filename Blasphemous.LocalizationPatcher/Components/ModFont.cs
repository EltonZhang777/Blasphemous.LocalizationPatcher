using Blasphemous.LocalizationPatcher.Extensions;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Files;
using System.Linq;
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
    /// Regular font asset used for the mod font
    /// </summary>
    public Font regularFont;

    public string TmpAssetName => info.fontName + "_tmp";
    public string RegularAssetName => info.fontName + "_regular";

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

        UnityEngine.Object[] assets;
#if DEBUG
        assets = ab.LoadAllAssets();
        ModLog.Warn($"All assets: ");
        foreach (UnityEngine.Object asset in assets)
        {
            ModLog.Warn($"  `{asset.name}`");
        }
        ModLog.Warn($"\n");
#endif

        // load regular asset
        assets = ab.LoadAllAssets<Font>();
        ModLog.Warn($"assets.Length: {assets.Length}");
        if (assets.Length == 1)
        {
            UnityEngine.Object asset = assets[0];
#if DEBUG
            ModLog.Warn($"acquired regular font asset {asset.name}");
#endif
            regularFont = asset as Font;
            regularFont.name = RegularAssetName;
        }

        // load tmp asset
        assets = ab.LoadAllAssets<TMP_FontAsset>();
        ModLog.Warn($"assets.Length: {assets.Length}");
        if (assets.Length == 1)
        {
            UnityEngine.Object asset = assets[0];
#if DEBUG
            ModLog.Warn($"acquired tmp font asset {asset.name}");
#endif
            tmpFont = asset as TMP_FontAsset;
            tmpFont.name = TmpAssetName;
        }
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
    public void AttachFontToLangauges()
    {
        foreach (string langName in info.supportedLanguages)
        {
            if (!Main.LocalizationPatcher.compiledLanguages.Exists(x => x.languageName == langName))
            {
                // if the language does not exist, remove it from the supportedLanguages list
                info.supportedLanguages.Remove(langName);
            }
            else
            {
                // attach the font to the language
                Main.LocalizationPatcher.compiledLanguages
                    .First(x => x.languageName == langName)
                    .modFonts.Add(this);
            }
        }
    }
}
