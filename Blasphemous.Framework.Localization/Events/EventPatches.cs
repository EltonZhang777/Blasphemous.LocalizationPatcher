using Framework.Managers;
using HarmonyLib;

namespace Blasphemous.Framework.Localization.Events;


[HarmonyPatch(typeof(EventManager), "SetFlag")]
internal class EventManager_SetFlag_FlagChangeEvent_Patch
{
    public static void Postfix(string id)
    {
        Main.LocalizationFramework.EventHandler.FlagChange(id);
    }
}