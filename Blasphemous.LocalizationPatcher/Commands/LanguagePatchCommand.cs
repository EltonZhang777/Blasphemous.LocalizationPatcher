using Blasphemous.CheatConsole;
using Blasphemous.LocalizationPatcher.Components;
using Blasphemous.ModdingAPI;
using Mono.Security.Cryptography;
using System;
using System.Collections.Generic;
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
        };
    }

    private void SubCommand_Help(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0)) 
            return;

        Write($"Available {CommandName} commands:");
        Write($"{CommandName} list : list all loaded language patches");
        Write($"{CommandName} list [applied/inactive]: list all applied/inactive language patches");
        Write($"{CommandName} apply [patchName] : apply the specified language patch");
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
                Write($"  {patch.patchName}");
            }
        }
        else
        {
            if (parameters[0].Equals("applied"))
            {
                Write($"All applied language patches: ");
                foreach (LanguagePatch patch in LanguagePatchRegister.Patches.Where(x => x.isApplied == true))
                {
                    Write($"  {patch.patchName}");
                }
            }
            else if (parameters[0].Equals("inactive"))
            {
                Write($"All inactive language patches: ");
                foreach (LanguagePatch patch in LanguagePatchRegister.Patches.Where(x => x.isApplied == false))
                {
                    Write($"  {patch.patchName}");
                }
            }
        }
    }

    private void SubCommand_Apply(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 1))
            return;

        if (!LanguagePatchRegister.Patches.ToList().Exists(x => x.patchName.Equals(parameters[0])))
        {
            Write($"Patch `{parameters[0]}` not found!");
            return;
        }

        int index = 0;
        List<LanguagePatch> languagePatches = LanguagePatchRegister.Patches.ToList();
        LanguagePatch targetPatch = languagePatches.First(x => x.patchName.Equals(parameters[0]));
        index = languagePatches.IndexOf(targetPatch);
        LanguagePatchRegister.AtIndex(index).CompileText();
        index = languagePatches.First(x => x.patchName.Equals(parameters[0])).LanguageIndex;
        Main.LocalizationPatcher.compiledLanguages[index].WriteAllTermsToGame();

        Write($"Successfully applied patch {parameters[0]}!");
        Write($"Patches applied through commands are only active until exiting game process");
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
