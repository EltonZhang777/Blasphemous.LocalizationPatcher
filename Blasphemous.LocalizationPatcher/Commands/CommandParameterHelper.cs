using System;
using System.Linq;
using System.Text;

namespace Blasphemous.LocalizationPatcher.Commands;

/// <summary>
/// Shared helpers for console command implementations.
/// </summary>
internal static class CommandParameterHelper
{
    /// <summary>
    /// Validate that the number of passed parameters matches one of the allowed lengths.
    /// On failure, writes an error message (through <paramref name="write"/>) listing the allowed lengths.
    /// </summary>
    internal static bool ValidateParameterList(Action<string> write, string[] parameters, params int[] validParameterLengths)
    {
        if (!validParameterLengths.Contains(parameters.Length))
        {
            StringBuilder sb = new();
            sb.Append("This command takes ");
            for (int i = 0; i < validParameterLengths.Length; i++)
            {
                sb.Append($"{validParameterLengths[i]} ");
                if (i != validParameterLengths.Length - 1)
                    sb.Append("or ");
            }
            sb.Append($"parameters.  You passed {parameters.Length}");
            write(sb.ToString());

            return false;
        }

        return true;
    }
}
