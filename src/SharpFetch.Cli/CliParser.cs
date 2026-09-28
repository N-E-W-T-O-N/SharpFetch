namespace SharpFetch.Cli;

public static class CliParser
{
    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        if (args == null || args.Length == 0) return options;

        for (int i = 0; i < args.Length; i++)
        {
            if (string.IsNullOrEmpty(args[i])) continue;
            ReadOnlySpan<char> arg = args[i].AsSpan();

            // Options must start with '-' or '--'
            if (arg[0] == '-')
            {
                ReadOnlySpan<char> trimmed = arg.TrimStart('-');

                // Check for '--key=value' format
                int eqIndex = trimmed.IndexOf('=');
                bool hasInlineValue = eqIndex >= 0;
                ReadOnlySpan<char> key = hasInlineValue ? trimmed[..eqIndex] : trimmed;
                ReadOnlySpan<char> inlineValue = hasInlineValue ? trimmed[(eqIndex + 1)..] : ReadOnlySpan<char>.Empty;

                // ====================================================
                // 1. Actions (Early Exits)
                // ====================================================
                if (key.Equals("v", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("version", StringComparison.OrdinalIgnoreCase))
                {
                    options.Action = CliAction.ShowVersion;
                    return options;
                }

                if (key.Equals("h", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("?", StringComparison.OrdinalIgnoreCase))
                {
                    options.Action = CliAction.ShowHelp;
                    return options;
                }

                if (key.Equals("list-modules", StringComparison.OrdinalIgnoreCase))
                {
                    options.Action = CliAction.ListModules;
                    return options;
                }

                // ====================================================
                // 2. Boolean Flags
                // ====================================================
                if (key.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("no-logo", StringComparison.OrdinalIgnoreCase))
                {
                    options.NoLogo = true;
                    continue;
                }

                if (key.Equals("no-color", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("disable-color", StringComparison.OrdinalIgnoreCase))
                {
                    options.DisableColor = true;
                    continue;
                }

                if (key.Equals("no-palette", StringComparison.OrdinalIgnoreCase))
                {
                    options.ShowColorPalette = false;
                    continue;
                }

                if (key.Equals("details", StringComparison.OrdinalIgnoreCase))
                {
                    options.ShowDetails = true;
                    continue;
                }

                // ====================================================
                // 3. String Options
                // ====================================================
                if (key.Equals("logo", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("l", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ExtractOptionValue(hasInlineValue, inlineValue, args, ref i);
                    if (val.IsEmpty)
                    {
                        return SetError(options, $"Option '{args[i]}' requires a value.");
                    }
                    options.CustomLogo = val.ToString();
                    continue;
                }

                if (key.Equals("color", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("accent", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ExtractOptionValue(hasInlineValue, inlineValue, args, ref i);
                    if (val.IsEmpty)
                    {
                        return SetError(options, $"Option '{args[i]}' requires a value.");
                    }
                    options.AccentColor = val.ToString();
                    continue;
                }

                if (key.Equals("config", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("c", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ExtractOptionValue(hasInlineValue, inlineValue, args, ref i);
                    if (val.IsEmpty)
                    {
                        return SetError(options, $"Option '{args[i]}' requires a value.");
                    }
                    options.ConfigPath = val.ToString();
                    continue;
                }

                // ====================================================
                // 4. List Options (Comma-separated or repeated)
                // ====================================================
                if (key.Equals("modules", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("module", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("m", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ExtractOptionValue(hasInlineValue, inlineValue, args, ref i);
                    if (val.IsEmpty)
                    {
                        return SetError(options, $"Option '{args[i]}' requires a value.");
                    }
                    ParseCommaSeparatedList(val, options.EnabledModules);
                    continue;
                }

                if (key.Equals("disable", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("hide", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ExtractOptionValue(hasInlineValue, inlineValue, args, ref i);
                    if (val.IsEmpty)
                    {
                        return SetError(options, $"Option '{args[i]}' requires a value.");
                    }
                    ParseCommaSeparatedList(val, options.DisabledModules);
                    continue;
                }

                // Unrecognized switch
                return SetError(options, $"Unrecognized option '{args[i]}'. Run 'sharpfetch --help' for available options.");
            }
            else
            {
                // Unexpected positional argument
                return SetError(options, $"Unexpected argument '{args[i]}'. Run 'sharpfetch --help' for available options.");
            }
        }

        return options;
    }

    private static CliOptions SetError(CliOptions options, string message)
    {
        options.Action = CliAction.ShowError;
        options.ErrorMessage = message;
        return options;
    }

    private static ReadOnlySpan<char> ExtractOptionValue(
        bool hasInlineValue,
        ReadOnlySpan<char> inlineValue,
        string[] args,
        ref int currentIndex)
    {
        // If inline '=' was provided (e.g. --key=val or --key=), do not steal the next argument
        if (hasInlineValue)
        {
            return inlineValue;
        }

        // Lookahead: consume next argument if present and not a switch (switches start with '-')
        if (currentIndex + 1 < args.Length &&
            !string.IsNullOrEmpty(args[currentIndex + 1]) &&
            !args[currentIndex + 1].StartsWith('-'))
        {
            currentIndex++;
            return args[currentIndex].AsSpan();
        }

        return ReadOnlySpan<char>.Empty;
    }

    /// <summary>
    /// Splits comma-separated values (e.g. "os,cpu,gpu") using spans, allocating only the final string tokens added to the list.
    /// </summary>
    private static void ParseCommaSeparatedList(ReadOnlySpan<char> span, List<string> targetList)
    {
        while (!span.IsEmpty)
        {
            int commaIdx = span.IndexOf(',');
            ReadOnlySpan<char> token = commaIdx >= 0 ? span[..commaIdx].Trim() : span.Trim();

            if (!token.IsEmpty)
            {
                targetList.Add(token.ToString());
            }

            if (commaIdx < 0) break;
            span = span[(commaIdx + 1)..];
        }
    }
}
