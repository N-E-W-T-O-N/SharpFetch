namespace SharpFetch.Cli;

public static class CliParser
{
    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();
        if (args == null || args.Length == 0) return options;

        for (int i = 0; i < args.Length; i++)
        {
            ReadOnlySpan<char> arg = args[i].AsSpan();

            if (arg.IsEmpty) continue;

            // Check if argument is a switch/option (starts with '-' or '/')
            if (arg[0] == '-' || arg[0] == '/')
            {
                // Strip leading dashes ('--', '-', '/')
                ReadOnlySpan<char> trimmed = arg.TrimStart("-/");

                // Check for '--key=value' format
                int eqIndex = trimmed.IndexOf('=');
                ReadOnlySpan<char> key = eqIndex >= 0 ? trimmed[..eqIndex] : trimmed;
                ReadOnlySpan<char> inlineValue = eqIndex >= 0 ? trimmed[(eqIndex + 1)..] : ReadOnlySpan<char>.Empty;

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

                // ====================================================
                // 3. String Options
                // ====================================================
                if (key.Equals("logo", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("l", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ExtractOptionValue(inlineValue, args, ref i);
                    if (!val.IsEmpty) options.CustomLogo = val.ToString();
                    continue;
                }

                if (key.Equals("color", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("accent", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ExtractOptionValue(inlineValue, args, ref i);
                    if (!val.IsEmpty) options.AccentColor = val.ToString();
                    continue;
                }

                if (key.Equals("config", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("c", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ExtractOptionValue(inlineValue, args, ref i);
                    if (!val.IsEmpty) options.ConfigPath = val.ToString();
                    continue;
                }

                // ====================================================
                // 4. List Options (Comma-separated or repeated)
                // ====================================================
                if (key.Equals("modules", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("module", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("m", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ExtractOptionValue(inlineValue, args, ref i);
                    if (!val.IsEmpty)
                    {
                        ParseCommaSeparatedList(val, options.EnabledModules);
                    }
                    continue;
                }

                if (key.Equals("disable", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("hide", StringComparison.OrdinalIgnoreCase))
                {
                    var val = ExtractOptionValue(inlineValue, args, ref i);
                    if (!val.IsEmpty)
                    {
                        ParseCommaSeparatedList(val, options.DisabledModules);
                    }
                    continue;
                }
            }
        }

        return options;
    }

    private static ReadOnlySpan<char> ExtractOptionValue(ReadOnlySpan<char> inlineValue, string[] args, ref int currentIndex)
    {
        if (!inlineValue.IsEmpty)
        {
            return inlineValue;
        }

        if (currentIndex + 1 < args.Length && !args[currentIndex + 1].StartsWith('-') && !args[currentIndex + 1].StartsWith('/'))
        {
            currentIndex++;
            return args[currentIndex].AsSpan();
        }

        return ReadOnlySpan<char>.Empty;
    }

    /// <summary>
    /// Splits comma-separated values (e.g., "os,cpu,gpu") with zero heap allocation per token before adding to list.
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
