using Blasphemous.LocalizationPatcher.Components;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using HarmonyLib;
using I2.Loc;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Blasphemous.LocalizationPatcher.Patches;

/// <summary>
/// Makes I2.Loc manager retrieve modded font assets.
/// </summary>
[HarmonyPatch(typeof(I2LocManager))]
internal class LocalizationManager_RetrieveModAsset_Patch
{
    [HarmonyPatch("FindAsset", [typeof(string)])]
    [HarmonyPrefix]
    public static bool Prefix(string value, ref UObject __result)
    {
        ModFont matchingModFont;

        // check if I2.Loc is querying for a regular font asset of a mod font
        matchingModFont = ModFontRegister.ModFonts.Where(x => x.TtfAssetName == value).FirstOrDefault();
        if (matchingModFont != null)
        {
            if (matchingModFont.ttfFont != null)
            {
                __result = matchingModFont.ttfFont;
                return false;
            }
        }

        // check if I2.Loc is querying for a tmp font asset of a mod font
        matchingModFont = ModFontRegister.ModFonts.Where(x => x.TmpAssetName == value).FirstOrDefault();
        if (matchingModFont != null)
        {
            if (matchingModFont.tmpFont != null)
            {
                __result = matchingModFont.tmpFont;
                return false;
            }
        }

        // check if I2.Loc is querying for a system font
        if (Main.LocalizationPatcher.SystemFontManager.TryGetLoadedSystemFont(value, out Font systemFont))
        {
            __result = systemFont;
            return false;
        }

        // no suitable font found, use vanilla implementation
        return true;
    }
}

[HarmonyPatch(typeof(LocalizeTarget_UnityUI_Text))]
internal class LocalizeTarget_UnityUI_Text__LoadModFontMaterial_Patch
{
    /// <summary>
    /// Make <c>Text</c> objects' localization use `None` material instead of font-related material
    /// </summary>
    [HarmonyPatch("DoLocalize")]
    [HarmonyPostfix]
    public static void UseModFontMaterial(
        LocalizeTarget_UnityUI_Text __instance,
        Localize cmp,
        string mainTranslation,
        string secondaryTranslation)
    {
        Text target = __instance.GetTarget(cmp);
        Font secondaryTranslatedObj = cmp.DoGetSecondaryTranslatedObj<Font>(ref mainTranslation, ref secondaryTranslation);
        // if vanilla game doesn't specify `secondaryTranslatedObj`, return early.
        if ((secondaryTranslatedObj == null) || (target == null))
        {
            return;
        }
        string fontName = secondaryTranslatedObj.name;

        // check if the font is modded font or system font, if not, return early.
        if ((ModFontRegister.ModFonts.FirstOrDefault(x => x.TtfAssetName == fontName) == null)
            && (!Main.LocalizationPatcher.SystemFontManager.HasSystemFont(fontName)))
        {
            return;
        }

        // use `None` material
        target.material = null;
    }
}