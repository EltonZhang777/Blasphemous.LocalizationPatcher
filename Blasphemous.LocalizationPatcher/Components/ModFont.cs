using Blasphemous.LocalizationPatcher.Extensions;
using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Files;
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

    public Material tmpMaterial;

    public Material ttfMaterial;

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

        Main.LogIfDebug($"Loading assetBundle!");
#if DEBUG
        StringBuilder sb = new();
        sb.AppendLine($"All assets in AssetBundle {ab.name}: ");
        foreach (UObject asset in ab.LoadAllAssets())
        {
            sb.AppendLine($"  asset name: `{asset.name}`; asset type: `{asset.GetType()}`");
        }
        sb.AppendLine();
        Main.LogIfDebug(sb.ToString());
#endif

        // load ttf assets
        ttfFont = ab.LoadAsset<Font>(info.ttfFontAssetName);
        ttfFont.name = RegularAssetName;
        ttfMaterial = ab.LoadAllAssets<Material>().FirstOrDefault(x => x.name == info.ttfMaterialAssetName);
        ttfMaterial.name = RegularAssetName;

        // WIP
        //// load tmp asset
        //tmpFont = ab.LoadAsset<TMP_FontAsset>(info.tmpFontAssetName);
        //tmpFont.name = TmpAssetName;
        //tmpMaterial = ab.LoadAsset<Material>(info.tmpMaterialAssetName);
        //tmpMaterial.name = TmpAssetName;
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
