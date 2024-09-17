using BepInEx;

namespace Blasphemous.LocalizationPatcher;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "2.4.1")]
internal class Main : BaseUnityPlugin
{
    public static LocalizationPatcher LocalizationPatcher { get; private set; }

    private void Start()
    {
        LocalizationPatcher = new LocalizationPatcher();
    }
}
