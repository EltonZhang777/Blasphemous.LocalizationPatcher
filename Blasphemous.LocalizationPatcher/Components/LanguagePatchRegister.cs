using Blasphemous.ModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.LocalizationPatcher.Components;

/// <summary>
/// Register handler for new language patches
/// </summary>
public static class LanguagePatchRegister
{

    internal static readonly List<LanguagePatch> _patches = new();
    internal static IEnumerable<LanguagePatch> Patches => _patches;
    internal static LanguagePatch AtIndex(int index) => _patches[index];
    internal static int Total => _patches.Count;

    /// <summary>
    /// Registers a new LanguagePatch 
    /// </summary>
    public static void RegisterLanguagePatch(
        this ModServiceProvider provider,
        LanguagePatch patch)
    {
        if (provider == null)
            return;

        // prevents repeated registering
        if (_patches.Any(x => x.patchName == patch.patchName))
            return;

        patch.parentModId = provider.RegisteringMod.Id;
        _patches.Add(patch);
        ModLog.Info($"Registered custom LanguagePatch: {patch.patchName}");
    }
}

