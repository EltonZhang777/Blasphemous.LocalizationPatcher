using Blasphemous.LocalizationPatcher.Components;
using Blasphemous.ModdingAPI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.LocalizationPatcher.Patches;

/// <summary>
/// Makes I2.Loc manager retrieve modded font assets.
/// </summary>
[HarmonyPatch(typeof(I2LocManager), "FindAsset", new Type[] { typeof(string) })]
class LocalizationManager_FindAsset_RetrieveModAsset_Patch
{

    public static bool Prefix(string value, ref UnityEngine.Object __result)
    {
#if DEBUG
        ModLog.Warn($"LocalizationManager.FindAsset({value})");
#endif
        List<ModFont> matchingModFonts = new();

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
        matchingModFonts = ModFontRegister.ModFonts.ToList().Where(x => x.RegularAssetName == value).ToList();
        if (matchingModFonts.Count == 1)
        {
            ModFont font = matchingModFonts[0];
            if (font.regularFont != null)
            {
                __result = font.regularFont;
                return false;
            }
        }

        // no mod font found, use vanilla implementation
        return true;
    }
}
