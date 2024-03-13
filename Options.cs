using System.Collections.Immutable;
using static grep.MyOptions;
using RegX = System.Text.RegularExpressions;

namespace grep;

internal static class Options
{
    public const string TextOff = "off";
    public const string TextOn = "on";

    public const string OptColor = "--color"; // ---------------------- TODO
    public const string OptFilesFrom = "--files-from";
    public const string OptCaseSensitive = "--case-sensitive";
    public const string OptWord = "--word";
    public const string OptLineNumber = "--line-number";
    public const string OptCountOnly = "--count-only";
    public const string OptFileMatch = "--file-match";
    public const string OptInvertMatch = "--invert-match";
    public const string OptPatternFile = "--pattern-file";
    public const string OptQuiet = "--quiet";
    public const string OptMaxCount = "--max-count";
    public const string OptShowFilename = "--show-filename";
    public const string OptFixedTextPattern = "--fixed-strings";
    public const string OptPause = "--pause";

    public static readonly IEnumerable<KeyValuePair<string, string>>
        ValueShortCuts =
        [
            new("-f", OptPatternFile),
            new("-m", OptMaxCount),
            new("-T", OptFilesFrom),
        ];

    public static readonly IEnumerable<KeyValuePair<string, string[]>>
        SwitchShortCuts =
        [
            new("-F", [OptFixedTextPattern, TextOn]),
            new("-c", [OptCountOnly, TextOn]),
            new("-h", [OptShowFilename, TextOff]),
            new("-i", [OptCaseSensitive, TextOff]),
            new("-l", [OptFileMatch, TextOn]),
            new("-n", [OptLineNumber, TextOn]),
            new("-p", [OptPause, TextOff]),
            new("-q", [OptQuiet, TextOn]),
            new("-v", [OptInvertMatch, TextOn]),
            new("-w", [OptWord, TextOn]),
        ];

    public static IEnumerable<FlagedArg> ToFlagedArgs(
        this IEnumerable<string> args, ArgType type,
        IEnumerable<KeyValuePair<string, string[]>> switchShortCuts,
        IEnumerable<KeyValuePair<string, string>> valueShortCuts)
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

        var mapShortcuts = switchShortCuts
            .Union(valueShortCuts
            .Select((it) => new KeyValuePair<string, string[]>(
                it.Key, [it.Value])))
            .ToImmutableDictionary((it) => it.Key, (it) => it.Value);

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

    static public Func<string, string, int, int> TextIndexOf
    { set; private get; } =
        (line, text, startIndex) => line.IndexOf(text, startIndex);

    static public IEnumerable<int> TextFindAllIndexOf_Impl(
        string line, string text)
    {
        int foundAt = 0;
        while ((foundAt = TextIndexOf(line, text, foundAt)) >= 0)
        {
            yield return foundAt;
            foundAt += text.Length;
        }
    }
    static public Func<string, string, IEnumerable<int>> TextFindAllIndexOf
    { get; private set; } = TextFindAllIndexOf_Impl;

    static public readonly IInvoke<string, string> PatternWordText
        = new SwitchInvoker<string, string>(OptWord, alterFor: true,
            init: Helper.Itself, alter: (arg) =>
            {
                if (false == arg.StartsWith(@"\b")) arg = @"\b" + arg;
                if (false == arg.EndsWith(@"\b")) arg = arg + @"\b";
                return arg;
            });

    static public readonly IInvoke<string, RegX.Regex> ToRegex
        = new SwitchInvoker<string, RegX.Regex>(OptCaseSensitive,
            init: (it) => new RegX.Regex(PatternWordText.Invoke(it)),
            alterFor: false, alterPost: (flag) =>
            {
                if (flag)
                {
                    TextIndexOf = (line, text,
                        startIndex) => line.IndexOf(text, startIndex);
                }
                else
                {
                    TextIndexOf = (line, text,
                        startIndex) => line.IndexOf(text, startIndex,
                        StringComparison.InvariantCultureIgnoreCase);
                }
            },
            alter: (it) => new RegX.Regex(PatternWordText.Invoke(it),
                RegX.RegexOptions.IgnoreCase));

    static public readonly IInvoke<IEnumerable<Match>, Match[]>
        MetaMatches = new SwitchInvoker<IEnumerable<Match>, Match[]>(
            OptInvertMatch, alterFor: true,
            init: (matches) => matches.ToArray(),
            alter: (matches) => matches.Any() ? [] : Match.ZeroOne);

    static public readonly IInvoke<string, Pattern> ToPattern =
        new SwitchInvoker<string, Pattern>(
            OptFixedTextPattern, alterFor: true,
            init: (it) => new Pattern(ToRegex.Invoke(it)),
            alter: (it) => new Pattern(it));

    static public readonly IInvoke<string[],
        (Func<string, Match[]>, IEnumerable<string>)>
        PatternsFrom = new ParseInvoker<string[],
            (Func<string, Match[]>, IEnumerable<string>)>(
            OptPatternFile, help: "PATTERN-FILE",
            extraHelp: $"For example, {nameof(grep)} {OptPatternFile} regex.txt ..",
            init: (args) =>
            {
                Pattern pattern;
                switch (args.Length)
                {
                    case 0:
                        throw new ConfigException("No pattern is found.");
                    case 1:
                        pattern = ToPattern.Invoke(args[0]);
                        return ((line) => pattern.Matches(line), []);
                    default:
                        pattern = ToPattern.Invoke(args[0]);
                        return ((line) => pattern.Matches(line), args.Skip(1));
                }
            },
            resolve: (opt, argsThe) =>
            {
                var files = argsThe.Distinct().Take(2).ToArray();
                if (files.Length > 1)
                {
                    throw new ConfigException(
                        $"Too many files ('{files[0]}','{files[1]}') to {opt.Name} are found!");
                }

                var patternFuncs = ReadAllLinesFrom(files[0], opt.Name)
                .Select((it) => it.Trim())
                .Where((it) => it.Length > 0)
                .Distinct()
                .Select((it) => ToPattern.Invoke(it))
                .Select((it) => it.Matches)
                .ToArray();

                Match[] matchFunc(string line) => patternFuncs
                .Select((matches) => matches(line))
                .FirstOrDefault((it) => it.Length > 0)
                ?? [];

                opt.SetImplementation((args) => (matchFunc, args));
            });

    static public IEnumerable<string> ReadAllLinesFrom(string path, string option)
    {
        return (path == "-")
            ? Helper.ReadAllLinesFromConsole(option)
            : Helper.ReadAllLinesFromFile(path, option);
    }

    static public readonly IInvoke<Ignore, IEnumerable<string>> FilesFrom
        = new ParseInvoker<Ignore, IEnumerable<string>>(
            OptFilesFrom, help: "FILES-FROM", init: (_) => [],
            extraHelp: $"For example, {nameof(grep)} {OptFilesFrom} cs-file.txt ..",
            resolve: (opt, argsThe) =>
            {
                var files = argsThe.Distinct().Take(2).ToArray();
                if (files.Length > 1)
                {
                    throw new ConfigException(
                        $"Too many files ('{files[0]}','{files[1]}') to {opt.Name} are found!");
                }

                opt.SetImplementation((_) => ReadAllLinesFrom(files[0], opt.Name)
                .Select((it) => it.Trim())
                .Where((it) => it.Length > 0));
            });

    public static readonly IParse[] Parsers = [
        (IParse)ToRegex,
        (IParse)Show.Filename,
        (IParse)Show.LineNumber,
        (IParse)Show.LogVerbose,
        (IParse)MetaMatches,
        (IParse)PatternWordText,
        (IParse)ToPattern,
        (IParse)Show.PauseMaker,
    ];

    public static readonly IParse[] ExtraParsers = [
        (IParse)Show.MaxFound,
        (IParse)PatternsFrom,
        (IParse)FilesFrom,
        (IParse)Show.FilenameOnly,
        (IParse)Show.FoundCount,
        ];

    static public IEnumerable<FlagedArg> Resolve(this IEnumerable<FlagedArg> args,
        IEnumerable<IParse> extraParsers)
    {
        return Parsers
            .Union(extraParsers)
            .Aggregate(seed: args, func: (acc, it) => it.Parse(acc));
    }
}
