using Blasphemous.LocalizationPatcher.Components;
using Blasphemous.LocalizationPatcher.Extensions;
using HarmonyLib;
using I2.Loc;
using System.Collections.Generic;
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
        //Main.LogIfDebug($"LocalizationManager.FindAsset({value})");
        List<ModFont> matchingModFonts = [];

        // check if I2.Loc is querying for a tmp font asset of a mod font
        matchingModFonts = ModFontRegister.ModFonts.ToList().Where(x => x.TmpAssetName == value).ToList();
        if (matchingModFonts.Count == 1)
        {
            ModFont font = matchingModFonts[0];
            if (font.tmpFont != null)
            {
                __result = font.tmpFont;
                return false;
            }
        }

        // check if I2.Loc is querying for a regular font asset of a mod font
        matchingModFonts = ModFontRegister.ModFonts.ToList().Where(x => x.TtfAssetName == value).ToList();
        if (matchingModFonts.Count == 1)
        {
            ModFont font = matchingModFonts[0];
            if (font.ttfFont != null)
            {
                __result = font.ttfFont;
                return false;
            }
        }

        // no mod font found, use vanilla implementation
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

        // check if the font is modded font, if not, return early.
        ModFont modFont = ModFontRegister.ModFonts.FirstOrDefault(x => x.TtfAssetName == secondaryTranslatedObj.name);
        if (modFont == null)
        {
            return;
        }

        target.material = null;
    }
}