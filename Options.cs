using System.Collections.Immutable;
using RegX = System.Text.RegularExpressions;

namespace grep;

internal static class Options
{
    public const string TextOff = "off";
    public const string TextOn = "on";

    public const string OptColor = "--color"; // ---------------------- TODO
    public const string OptFilesFrom = "--files-from"; // ------------- TODO
    public const string OptCaseSensitive = "--case-sensitive"; // ----- TODO
    public const string OptWord = "--word"; // ------------------------ TODO
    public const string OptLineNumber = "--line-number";
    public const string OptCountOnly = "--count-only";
    public const string OptFileMatch = "--file-match";
    public const string OptInvertMatch = "--invert-match"; // --------- TODO
    public const string OptRegexFile = "--regex-file"; // ------------- TODO
    public const string OptQuiet = "--quiet"; // ---------------------- TODO
    public const string OptMaxCount = "--max-count";
    public const string OptShowFilename = "--show-filename";
    public const string OptFixedTextFrom = "--fixed-text-file"; // ---- TODO
    public const string OptPause = "--pause"; // ---------------------- TODO

    public static ImmutableDictionary<string, string[]> ExpandStrings =
        new Dictionary<string, string[]>()
        {
            ["-f"] = [OptRegexFile],
            ["-F"] = [OptFixedTextFrom],
            ["-m"] = [OptMaxCount],
            ["-T"] = [OptFilesFrom],

            ["-c"] = [OptCountOnly, TextOn],
            ["-h"] = [OptShowFilename, TextOff],
            ["-i"] = [OptCaseSensitive, TextOff],
            ["-l"] = [OptFileMatch, TextOn],
            ["-n"] = [OptLineNumber, TextOn],
            ["-p"] = [OptPause, TextOff],
            ["-q"] = [OptQuiet, TextOn],
            ["-v"] = [OptInvertMatch, TextOn],
            ["-w"] = [OptWord, TextOn],
        }.ToImmutableDictionary();

    public static IEnumerable<FlagedArg> ToFlagedArgs(
        this IEnumerable<string> args, ArgType type,
        ImmutableDictionary<string, string[]> mapShortcuts)
    {
        var itrThe = args.GetEnumerator();
        IEnumerable<string> SeparateCombiningShortcut()
        {
            while (itrThe.MoveNext())
            {
                var current = itrThe.Current;
                if (current.StartsWith("--"))
                {
                    yield return current;
                }
                else if (current.StartsWith("-") && current.Length > 2)
                {
                    if ('0' <= current[1] && current[1] <= '9')
                    {
                        yield return current;
                    }
                    else
                    {
                        foreach (var charThe in current.Skip(1))
                        {
                            yield return "-" + charThe;
                        }
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }

        foreach (var arg in SeparateCombiningShortcut())
        {
            if (arg.Length == 2 && arg[0] == '-')
            {
                if (mapShortcuts.TryGetValue(arg, out var expands))
                {
                    foreach (var expand in expands)
                    {
                        yield return new(false, type, expand);
                    }
                }
                else
                {
                    yield return new(false, type, arg);
                }
            }
            else
            {
                yield return new(false, type, arg);
            }
        }
    }

    public static FlagedArg[] ToFlagedArgs(this string arg,
        ArgType type = ArgType.CommandLine)
    {
        return [new(true, type, arg)];
    }

    static public Func<string, string, bool> TextContains
    { get; private set; } = (line, text) => line.Contains(text);

    static public readonly IInvoke<string, RegX.Regex> ToRegex
        = new MyOptions.SwitchInvoker<string, RegX.Regex>(
            OptCaseSensitive, init: (it) => new RegX.Regex(it),
            alterFor: false, alterWhen: (flag) =>
            {
                if (flag)
                {
                    TextContains = (line, text) => line.Contains(text);
                }
                else
                {
                    TextContains = (line, text) => line.Contains(text,
                        StringComparison.InvariantCultureIgnoreCase);
                }
            },
            alter: (it) => new RegX.Regex(it,
                RegX.RegexOptions.IgnoreCase));

    static readonly IParse[] Parsers = [
        (IParse)ToRegex,
        (IParse)Show.Filename,
        (IParse)Show.LineNumber,
        (IParse)Show.FoundCount,
        (IParse)Show.LogVerbose,
        (IParse)Show.MyTake,
        (IParse)Show.FilenameOnly,
    ];

    static public IEnumerable<FlagedArg> Resolve(this IEnumerable<FlagedArg> args)
    {
        return Parsers.Aggregate(seed: args, func: (acc, it) => it.Parse(acc));
    }
}
