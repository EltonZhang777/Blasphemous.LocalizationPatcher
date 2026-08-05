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
            { "revert", Subcommand_Revert },
            { "apply", SubCommand_Apply },
            { "listsystem", SubCommand_ListSystemFonts },
            { "applysystem", SubCommand_ApplySystemFont }
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
        Write($"{CommandName} apply [fontName] [languageName]: apply the specified mod font to the specified language");
        Write($"{CommandName} listsystem : list all system fonts installed on this PC");
        Write($"{CommandName} applysystem [fontName] [languageName]: apply the specified system font to the specified language");
        Write($"{CommandName} revert [languageName]: remove applied mod fonts for the specified language");
        Write($"(Use underscore `_` to represent spaces in font and language names.)");
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

        string fontName = parameters[0].Replace("_", " ");
        string languageName = parameters[1].Replace("_", " ");

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

        Write($"Successfully applied mod font `{fontName}` to `{languageName}`!");
    }

    private void SubCommand_ListSystemFonts(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 0))
            return;

        Write($"All system fonts on this PC: ");
        foreach (string fontName in Main.LocalizationPatcher.SystemFontManager.AllSystemFonts)
        {
            Write($"  {fontName}");
        }
    }

    private void SubCommand_ApplySystemFont(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 2))
            return;

        string fontName = parameters[0].Replace("_", " ");
        string languageName = parameters[1].Replace("_", " ");

        // validate font and language's existence
        if (!Main.LocalizationPatcher.SystemFontManager.HasSystemFont(fontName))
        {
            Write($"System font `{fontName}` not found on this PC!");
            return;
        }
        if (!Main.LocalizationPatcher.compiledLanguages.Exists(x => x.languageName == languageName))
        {
            Write($"Language `{languageName}` not found!");
            return;
        }

        Main.LocalizationPatcher.SystemFontManager.TryApplySystemFont(fontName, languageName);

        Write($"Successfully applied system font `{fontName}` to `{languageName}`!");
    }

    private void Subcommand_Revert(string[] parameters)
    {
        if (!ValidateParameterList(parameters, 1))
            return;

        string languageName = parameters[0].Replace("_", " ");

        // validate language's existence
        if (!Main.LocalizationPatcher.compiledLanguages.Exists(x => x.languageName == languageName))
        {
            Write($"Language `{languageName}` not found!");
            return;
        }

        CompiledLanguage targetCompiledLanguage = Main.LocalizationPatcher.compiledLanguages.First(x => x.languageName == languageName);

        // determine vanilla default fonts for this language
        string regularFont;
        string tmpFont;
        if (LocalizationPatcher.IsVanillaLanguage(languageName))
        {
            regularFont = LocalizationPatcher.vanillaRegularFontNames[languageName];
            tmpFont = LocalizationPatcher.vanillaTmpFontNames[languageName];
        }
        else
        {
            regularFont = "MajesticExtended_Pixel_Scroll";
            tmpFont = "MajesticExtended_FullLatin";
        }

        // reset font terms to vanilla defaults
        targetCompiledLanguage.TryUpdateTerm("UI/FONT", regularFont, PatchTerm.TermOperation.ReplaceAll);
        targetCompiledLanguage.TryUpdateTerm("UI/FONT_SCROLL", regularFont, PatchTerm.TermOperation.ReplaceAll);
        targetCompiledLanguage.TryUpdateTerm("UI/FONT_TEXTMESH_PRO", tmpFont, PatchTerm.TermOperation.ReplaceAll);

        targetCompiledLanguage.WriteTermsToGame(["UI/FONT", "UI/FONT_SCROLL", "UI/FONT_TEXTMESH_PRO"]);

        // force localize the language in I2.Loc to apply the font change
        I2LocManager.SetLanguageAndCode(languageName, I2LocManager.GetLanguageCode(languageName), true, true);

        Write($"Successfully reverted all mod fonts for `{languageName}`!");
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
