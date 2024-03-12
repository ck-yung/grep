using System.Collections.Generic;
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
    public const string OptWord = "--word"; // ------------------------ TODO
    public const string OptLineNumber = "--line-number";
    public const string OptCountOnly = "--count-only";
    public const string OptFileMatch = "--file-match";
    public const string OptInvertMatch = "--invert-match";
    public const string OptPatternFile = "--pattern-file";
    public const string OptQuiet = "--quiet";
    public const string OptMaxCount = "--max-count";
    public const string OptShowFilename = "--show-filename";
    public const string OptFixedTextPattern = "--fixed-strings";
    public const string OptPause = "--pause"; // ---------------------- TODO

    public static ImmutableDictionary<string, string[]> ExpandStrings =
        new Dictionary<string, string[]>()
        {
            ["-f"] = [OptPatternFile],
            ["-m"] = [OptMaxCount],
            ["-T"] = [OptFilesFrom],

            ["-F"] = [OptFixedTextPattern, TextOn],
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
        = new SwitchInvoker<string, RegX.Regex>(
            OptCaseSensitive, init: (it) => new RegX.Regex(it),
            alterFor: false, alterPost: (flag) =>
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

    #region Matching Functions
    static public Func<Func<string, string, bool>, Func<string, string, bool>>
        MatchText { get; private set; } = Helper.Itself;

    static public readonly IInvoke<RegX.Regex, Func<string, Match[]>>
        MatchRegex = new SwitchInvoker<RegX.Regex, Func<string, Match[]>>(
            OptInvertMatch, alterFor: true,

            init: (regex) =>
            (it) => regex.Matches(it)
            .OfType<RegX.Match>()
            .Where((it) => it.Success)
            .Select((it) => new Match(it.Index, it.Length))
            .ToArray(),

            alterPost: (flag) =>
            {
                if (flag)
                {
                    MatchText = (func) =>
                    (line, text) => true != func(line, text);
                }
                else
                {
                    MatchText = Helper.Itself;
                }
            },

            alter: (regex) =>
            {
                bool b2(string it) => regex.Matches(it)
                .OfType<RegX.Match>()
                .Where((it) => it.Success).Any();
                return (it) => b2(it) ? [] : Match.ZeroOne;
            });
    #endregion

    static public readonly IInvoke<string, Pattern> ToPattern =
        new SwitchInvoker<string, Pattern>(
            OptFixedTextPattern, alterFor: true,
            init: (it) => new Pattern( Options.ToRegex.Invoke(it)),
            alter: (it) => new Pattern(it));

    static public readonly IInvoke<string[], (Func<string, Match[]>, IEnumerable<string>)>
        PatternsFrom = new ParseInvoker<string[], (Func<string, Match[]>, IEnumerable<string>)>(
            OptPatternFile,
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
            OptFilesFrom, init: (_) => [],
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

    static readonly IParse[] Parsers = [
        (IParse)ToRegex,
        (IParse)Show.Filename,
        (IParse)Show.LineNumber,
        (IParse)Show.FoundCount,
        (IParse)Show.LogVerbose,
        (IParse)Show.MyTake,
        (IParse)Show.FilenameOnly,
        (IParse)MatchRegex,
        (IParse)ToPattern,
        (IParse)PatternsFrom,
        (IParse)FilesFrom,
        (IParse)Show.PauseMaker,
    ];

    static public IEnumerable<FlagedArg> Resolve(this IEnumerable<FlagedArg> args)
    {
        return Parsers.Aggregate(seed: args, func: (acc, it) => it.Parse(acc));
    }

    static public IEnumerable<string> OptNames()
    {
        return Parsers.Select((it) => it.Name);
    }
}
