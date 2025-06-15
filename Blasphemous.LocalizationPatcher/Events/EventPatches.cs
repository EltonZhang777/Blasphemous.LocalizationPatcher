using Framework.Managers;
using HarmonyLib;

namespace Blasphemous.LocalizationPatcher.Events;


[HarmonyPatch(typeof(EventManager), "SetFlag")]
internal class EventManager_SetFlag_FlagChangeEvent_Patch
{
    public static void Postfix(string id)
    {
        Main.LocalizationPatcher.EventHandler.FlagChange(id);
    }
}