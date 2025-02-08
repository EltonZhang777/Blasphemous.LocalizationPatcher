using Blasphemous.ModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.Framework.Localization.Components;

/// <summary>
/// Register handler for new language patches
/// </summary>
public static class LanguagePatchRegister
{

    private static readonly List<LanguagePatch> _patches = new();
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
        string name = patch.patchName;
        if (_patches.Any(x => x.patchName == name))
            return;

        patch.parentModId = provider.RegisteringMod.Id;
        _patches.Add(patch);
        ModLog.Info($"Registered custom LanguagePatch: {patch.patchName}");
    }

    internal static void SortPatchOrder()
    {
        _patches.Sort(PatchOrderSorter);
    }

    /// <summary>
    /// Sort patching order first by mod order, then by patchOrder
    /// </summary>
    private static int PatchOrderSorter(LanguagePatch x, LanguagePatch y)
    {
        if (x == null || y == null)
            return 0;
        else if (x != null && y == null)
            return 1;
        else if (x == null && y != null)
            return -1;

        // x != null && y != null
        // Compare mod order first
        int xModIndex = Main.LocalizationFramework.config.patchingModOrder.IndexOf(x.parentModId);
        int yModIndex = Main.LocalizationFramework.config.patchingModOrder.IndexOf(y.parentModId);
        if (xModIndex > yModIndex)
            return 1;
        else if (xModIndex < yModIndex)
            return -1;

        // xModIndex == yModIndex
        // Mod order same, compare order within same mod
        // patch with bigger patch order is registered first
        return (x.patchOrder.CompareTo(y.patchOrder)) * -1;
    }
}

