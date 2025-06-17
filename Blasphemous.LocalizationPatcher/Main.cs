using BepInEx;
using System.Collections.Generic;
using System.Linq;

namespace Blasphemous.LocalizationPatcher;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "2.4.1")]
[BepInDependency("Blasphemous.CheatConsole", "1.0.1")]
internal class Main : BaseUnityPlugin
{
    public static LocalizationPatcher LocalizationPatcher { get; private set; }

    private void Start()
    {
        LocalizationPatcher = new LocalizationPatcher();
    }

    /// <summary>
    /// Validates the sorting order of the elements in the list and resolves any discrepancies.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="orderList">the list containing the order that needs to be validated and modified if necessary</param>
    /// <param name="elementsList">the list containing all the elements</param>
    internal static void ValidateAndResolveSortingOrder<T>(ref List<T> orderList, List<T> elementsList)
    {
        // make sure both lists are distinct
        elementsList = elementsList.Distinct().ToList();
        orderList = orderList.Distinct().ToList();

        // if orderList is missing any element from elementsList, add it to the end
        foreach (T element in elementsList)
        {
            if (!orderList.Contains(element))
            {
                orderList.Add(element);
            }
        }

        // if orderList contains elements not in elementList, remove them
        orderList = orderList.Where(x => elementsList.Contains(x)).ToList();
    }
}
