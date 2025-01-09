using BepInEx;

namespace Blasphemous.Framework.Localization;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "2.4.1")]
[BepInDependency("Blasphemous.CheatConsole", "1.0.1")]
internal class Main : BaseUnityPlugin
{
    public static LocalizationFramework LocalizationFramework { get; private set; }

    private void Start()
    {
        LocalizationFramework = new LocalizationFramework();
    }
}
