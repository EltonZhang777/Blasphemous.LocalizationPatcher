using Blasphemous.CheatConsole;
using Blasphemous.LocalizationPatcher.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Blasphemous.LocalizationPatcher.Commands;

internal class LanguagePatchCommand : ModCommand
{
    protected override string CommandName => "languagepatch";

    protected override bool AllowUppercase => true;

    protected override Dictionary<string, Action<string[]>> AddSubCommands()
    {
        return new()
        {
            { "help", SubCommand_Help },
            { "list", SubCommand_List },
            { "apply", SubCommand_Apply },
            { "remove", SubCommand_Remove },
            { "removeall", SubCommand_RemoveAll },
            { "export", SubCommand_ExportToJson }
        };
    }

    private void SubCommand_Help(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0))
            return;

        Write($"Available {CommandName} commands:");
        Write($"{CommandName} list : list all loaded language patches");
        Write($"{CommandName} list [applied/inactive] : list all applied/inactive language patches");
        Write($"{CommandName} apply [patchName] : apply the specified language patch");
        Write($"{CommandName} remove [patchName] : remove the specified language patch");
        Write($"{CommandName} removeall : remove all applied language patches");
        Write($"{CommandName} export [patchName]: export the specified language patch to `Modding/content/{Main.LocalizationPatcher.Name}/[patchName].json`");
    }

    private void SubCommand_List(string[] parameters)
    {
        if (!ValidateParameterList(parameters, [0, 1]))
            return;

        if (parameters.Length == 0)
        {
            Write($"All loaded language patches: ");
            foreach (LanguagePatch patch in LanguagePatchRegister.Patches)
            {
                Write($"  {patch.patchName} | type: {patch.patchType} | status: {(patch.isApplied ? "applied" : "inactive")}");
            }
        }
        else
        {
            if (parameters[0].Equals("applied"))
            {
                Write($"All applied language patches: ");
                foreach (LanguagePatch patch in LanguagePatchRegister.Patches.Where(x => x.isApplied == true))
                {
                    Write($"  {patch.patchName} | type: {patch.patchType} | status: applied");
                }
            }
            else if (parameters[0].Equals("inactive"))
            {
                Write($"All inactive language patches: ");
                foreach (LanguagePatch patch in LanguagePatchRegister.Patches.Where(x => x.isApplied == false))
                {
                    Write($"  {patch.patchName} | type: {patch.patchType} | status: inactive");
                }
            }
        }
    }

    private void SubCommand_Apply(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 1))
            return;

        // validate the specified patch's existence
        if (!LanguagePatchRegister.Patches.ToList().Exists(x => x.patchName.Equals(parameters[0])))
        {
            Write($"Patch `{parameters[0]}` not found!");
            return;
        }

        // apply the patch to the specified language
        LanguagePatch targetPatch = LanguagePatchRegister.AtName(parameters[0]);

        // prevent re-applying an already applied patch, which would stack
        // Prefix/Suffix terms on top of themselves
        if (targetPatch.isApplied)
        {
            Write($"Patch `{parameters[0]}` is already applied!");
            return;
        }

        targetPatch.CompileText();
        targetPatch.CompiledLanguage.WritePatchToGame(targetPatch.patchName);

        Write($"Successfully applied patch {parameters[0]}!");
        Write($"Manual patches applied through commands are only active until exiting game process");
    }

    private void SubCommand_ExportToJson(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 1))
            return;

        if (!LanguagePatchRegister.Patches.ToList().Exists(x => x.patchName.Equals(parameters[0])))
        {
            Write($"Patch `{parameters[0]}` not found!");
            return;
        }

        LanguagePatch targetPatch = LanguagePatchRegister.Patches.ToList().First(x => x.patchName.Equals(parameters[0]));
        File.WriteAllText(
            Main.LocalizationPatcher.FileHandler.ContentFolder + $"{targetPatch.patchName}.json",
            JsonConvert.SerializeObject(targetPatch, Formatting.Indented));
        Write($"Successfully exported selected language patch to `Modding/content/{Main.LocalizationPatcher.Name}/{targetPatch.patchName}.json`");
    }

    private void SubCommand_Remove(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 1))
            return;

        // validate the specified patch's existence
        if (!LanguagePatchRegister.Patches.ToList().Exists(x => x.patchName.Equals(parameters[0])))
        {
            Write($"Patch `{parameters[0]}` not found!");
            return;
        }

        // validate the specified patch's applied state
        LanguagePatch targetPatch = LanguagePatchRegister.AtName(parameters[0]);
        if (!targetPatch.isApplied)
        {
            Write($"Patch `{parameters[0]}` is not currently applied!");
            return;
        }

        // remove the patch from the game
        targetPatch.CompiledLanguage.RemovePatchFromGame(targetPatch.patchName);

        Write($"Successfully removed patch `{parameters[0]}` from game!");
        Write($"OnInitialize and OnFlag patches removed through commands are only deactivated until exiting game process");
    }

    /// <summary>
    /// Remove all applied language patches from the game by resetting all languages to default.
    /// </summary>
    private void SubCommand_RemoveAll(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0))
            return;

        foreach (CompiledLanguage lang in Main.LocalizationPatcher.compiledLanguages)
        {
            lang.RestoreOriginalTermsToGame();
        }
        Write($"Successfully removed all applied language patches from game!");
    }

    private bool ValidateParameterList(string[] parameters, List<int> validParameterLengths)
    {
        if (!validParameterLengths.Contains(parameters.Length))
        {
            StringBuilder sb = new();
            sb.Append($"This command takes ");
            for (int i = 0; i < validParameterLengths.Count; i++)
            {
                sb.Append($"{i} ");
                if (i != validParameterLengths.Count - 1)
                    sb.Append("or ");
            }
            sb.Append($"parameters.  You passed {parameters.Length}");
            Write(sb.ToString());

            return false;
        }

        return true;
    }
}
