using System.Collections.Generic;

namespace Blasphemous.LocalizationPatcher.Components;

/// <summary>
/// Serializable info of mod font
/// </summary>
public class ModFontInfo
{
    /// <summary>
    /// Name of the font
    /// </summary>
    public string fontName = "";

    /// <summary>
    /// File name of the AssetBundle containing the font
    /// </summary>
    public string fileName = "";

    public string ttfFontAssetName = "";

    public string ttfMaterialAssetName = "";

    public string tmpFontAssetName = "";

    public string tmpMaterialAssetName = "";

    /// <summary>
    /// The languages that this font can display
    /// </summary>
    public List<string> supportedLanguages = [];
}
