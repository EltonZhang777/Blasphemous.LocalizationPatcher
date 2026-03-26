using HarmonyLib;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Blasphemous.LocalizationPatcher.Extensions;
internal static class I2Extensions
{
    internal static T DoGetSecondaryTranslatedObj<T>(
        this Localize localize,
        ref string mainTranslation,
        ref string secondaryTranslation) where T : UObject
    {
        string text;
        string text2;
        localize.DoDeserializeTranslation(mainTranslation, out text, out text2);
        T t = (T)((object)null);
        if (!string.IsNullOrEmpty(text2))
        {
            t = localize.DoGetObject<T>(text2);
            if (t != null)
            {
                mainTranslation = text;
                secondaryTranslation = text2;
            }
        }
        if (t == null)
        {
            t = localize.DoGetObject<T>(secondaryTranslation);
        }
        return t;
    }

    internal static void DoDeserializeTranslation(
        this Localize localize,
        string translation,
        out string value,
        out string secondary)
    {
        if (!string.IsNullOrEmpty(translation) && translation.Length > 1 && translation[0] == '[')
        {
            int num = translation.IndexOf(']');
            if (num > 0)
            {
                secondary = translation.Substring(1, num - 1);
                value = translation.Substring(num + 1);
                return;
            }
        }
        value = translation;
        secondary = string.Empty;
    }

    internal static T DoGetObject<T>(
        this Localize localize,
        string Translation) where T : UObject
    {
        if (string.IsNullOrEmpty(Translation))
        {
            return (T)((object)null);
        }
        T translatedObject = localize.DoGetTranslatedObject<T>(Translation);
        if (translatedObject == null)
        {
            translatedObject = localize.DoGetTranslatedObject<T>(Translation);
        }
        return translatedObject;
    }

    internal static T DoGetTranslatedObject<T>(
        this Localize localize,
        string Translation) where T : UObject
    {
        return localize.FindTranslatedObject<T>(Translation);
    }
}
