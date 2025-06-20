using Blasphemous.ModdingAPI.Files;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Blasphemous.LocalizationPatcher.Extensions;

internal static class FileHandlerExtensions
{
    internal static string GetDataPath(this FileHandler fileHandler)
    {
        return Traverse.Create(fileHandler).Field("dataPath").GetValue<string>();
    }

    internal static string[] GetAllDataFileNames(this FileHandler fileHandler)
    {
        return Directory.GetFiles(fileHandler.GetDataPath()).Select(x => Path.GetFileName(x)).ToArray();
    }

    internal static T LoadDataAsJson<T>(this FileHandler fileHandler, string fileName)
    {
        if (!fileHandler.LoadDataAsJson(fileName, out T result))
        {
            throw new ArgumentException($"Failed to load {fileName} to JSON of type {typeof(T)}!");
        }
        return result;
    }

    internal static bool LoadContentAsJson<T>(this FileHandler fileHandler, string fileName, out T output)
    {
        if (ReadFileContents(fileHandler, fileHandler.ContentFolder + fileName, out var output2))
        {
            output = JsonConvert.DeserializeObject<T>(output2);
            return true;
        }

        output = default(T);
        return false;
    }

    internal static void WriteJsonToContent(this FileHandler fileHandler, string fileName, object obj)
    {
        File.WriteAllText(
            Path.Combine(fileHandler.ContentFolder, fileName),
            JsonConvert.SerializeObject(obj, Formatting.Indented));
    }

    internal static bool LoadDataAsAssetBundle(this FileHandler fileHandler, string fileName, out AssetBundle assetBundle)
    {
        assetBundle = AssetBundle.LoadFromFile(Path.Combine(GetDataPath(fileHandler), fileName));
        return assetBundle != null;
    }

    private static bool ReadFileContents(this FileHandler fileHandler, string path, out string output)
    {
        if (File.Exists(path))
        {
            output = File.ReadAllText(path);
            return true;
        }

        output = null;
        return false;
    }
}
