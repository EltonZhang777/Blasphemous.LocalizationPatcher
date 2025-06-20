using Blasphemous.CheatConsole;
using Blasphemous.LocalizationPatcher.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blasphemous.LocalizationPatcher.Commands;

internal class ModFontCommand : ModCommand
{
    protected override string CommandName => "font";

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
        Write($"{CommandName} list : list all loaded mod fonts");
        Write($"{CommandName} list [languageName] : list all mod fonts applicable to the specified language");
        Write($"{CommandName} list [languageName] [current]: show the currently used fonts of the specified language");
        Write($"{CommandName} apply [fontName] [languageName]: apply the specified font to the speicified language");
    }


    private void SubCommand_List(string[] parameters)
    {
        if (!ValidateParameterList(parameters, [0, 1, 2]))
            return;

        if (parameters.Length == 0)
        {
            Write($"All loaded mod fonts: ");
            foreach (ModFont modFont in ModFontRegister.ModFonts)
            {
                Write($"  {modFont.info.fontName}");
            }
        }
        else if (parameters.Length == 1)
        {
            string languageName = parameters[0];
            if (!Main.LocalizationPatcher.compiledLanguages.Exists(x => x.languageName == languageName))
            {
                Write($"Language `{languageName}` not found!");
                return;
            }

            Write($"All loaded mod fonts for `{languageName}`: ");
            CompiledLanguage targetCompiledLanguage = Main.LocalizationPatcher.compiledLanguages.First(x => x.languageName == languageName);
            foreach (ModFont modFont in targetCompiledLanguage.modFonts)
            {
                Write($"  {modFont.info.fontName}");
            }
        }
        else if (parameters.Length == 2)
        {
            string languageName = parameters[0];
            if (!Main.LocalizationPatcher.compiledLanguages.Exists(x => x.languageName == languageName))
            {
                Write($"Language `{languageName}` not found!");
                return;
            }

            if (!parameters[1].Equals("current"))
            {
                Write($"Invalid second parameter `{parameters[1]}`");
                return;
            }
            Write($"Currently used fonts for `{languageName}`: ");
            CompiledLanguage targetCompiledLanguage = Main.LocalizationPatcher.compiledLanguages.First(x => x.languageName == languageName);
            targetCompiledLanguage.GetCurrentFonts(out string regularFont, out string tmpFont);
            Write($"  regular font: {regularFont}");
            Write($"  TextMeshPro font: {tmpFont}");
            return;
        }
    }

    private void SubCommand_Apply(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 2))
            return;

        string fontName = parameters[0];
        string languageName = parameters[1];

        // validate font and language's existence
        if (!ModFontRegister.ModFonts.ToList().Exists(x => x.info.fontName == fontName))
        {
            Write($"Mod font `{fontName}` not found!");
            return;
        }
        if (!Main.LocalizationPatcher.compiledLanguages.Exists(x => x.languageName == languageName))
        {
            Write($"Language `{languageName}` not found!");
            return;
        }

        ModFont targetFont = ModFontRegister.AtName(fontName);
        CompiledLanguage targetCompiledLanguage = Main.LocalizationPatcher.compiledLanguages.First(x => x.languageName == languageName);

        // validate the font is applicable to the language
        if (!targetCompiledLanguage.modFonts.Exists(x => x.info.fontName == fontName))
        {
            Write($"Font `{fontName}` not applicable to `{languageName}`!");
            return;
        }

        // apply the font to the specified language
        targetCompiledLanguage.ApplyFontToGame(targetFont);

        Write($"Successfully applied font `{fontName}` to `{languageName}`!");
        Write($"Fonts applied through commands are only active until exiting game process");
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
