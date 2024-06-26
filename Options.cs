using System.Collections.Immutable;
using RegX = System.Text.RegularExpressions;
using static grep.MyOptions;
using Dir;

namespace grep;

internal static partial class Options
{
    public const string TextOff = "off";
    public const string TextOn = "on";
    public const string HintOnOff = "on | off";

    public const string TextColor = "--color";
    public const string TextFilesFrom = "--files-from";
    public const string TextCaseSensitive = "--case-sensitive";
    public const string TextWord = "--word";
    public const string TextLineNumber = "--line-number";
    public const string TextCountOnly = "--count-only";
    public const string TextFileMatch = "--file-match";
    public const string TextInvertMatch = "--invert-match";
    public const string TextPatternFile = "--pattern-file";
    public const string TextQuiet = "--quiet";
    public const string TextMaxCount = "--max-count";
    public const string TextShowFilename = "--show-filename";
    public const string TextFixedTextPattern = "--fixed-strings";
    public const string TextPause = "--pause";
    public const string TextTotal = "--total";
    public const string TextSubDir = "--sub-dir";
    public const string TextExclFile = "--excl-file";
    public const string TextExclDir = "--excl-dir";
    public const string TextTrim = "--trim";
    public const string TextFilenameCaseSensitive = "--filename-case-sensitive";
    public const string TextMapShortcut = "--map-shortcut";
    public const string TextSkipArg = "--skip-arg";
    public const string TextSplitFileByComma = "--split-file-by-comma";
    public const string TextMaxReportFileNotFound = "--max-file-not-found";
    public const string TextSearch = "--search";

    public static readonly IEnumerable<KeyValuePair<string, string[]>>
        NonEnvirShortCuts =
        [
            new("-f", [TextPatternFile]),
            new("-m", [TextMaxCount]),
            new("-T", [TextFilesFrom]),
            new("-c", [TextCountOnly, TextOn]),
            new("-l", [TextFileMatch, TextOn]),
            new("-v", [TextInvertMatch, TextOn]),
            new("-r", [TextSubDir, TextOn]),
            new("-e", [TextSearch]),
        ];

    public static readonly IEnumerable<KeyValuePair<string, string[]>>
        ShortCuts =
        [
            new("-F", [TextFixedTextPattern, TextOn]),
            new("-h", [TextShowFilename, TextOff]),
            new("-H", [TextShowFilename, TextOn]),
            new("-i", [TextCaseSensitive, TextOff]),
            new("-n", [TextLineNumber, TextOn]),
            new("-N", [TextLineNumber, TextOff]),
            new("-p", [TextPause, TextOff]),
            new("-q", [TextQuiet, TextOn]),
            new("-w", [TextWord, TextOn]),
            new("-x", [TextExclFile]),
            new("-X", [TextExclDir]),
        ];

    public static readonly SortedSet<string> QuickHelpExclShortcuts =
        ["-N", "-e"];

    public static ImmutableDictionary<string, string> MappedShortcut
    {
        get => MappedShortcutImp.ToImmutableDictionary();
    }
    static readonly Dictionary<string, string> MappedShortcutImp = [];

    public static IEnumerable<FlagedArg> ToFlagedArgs(
        this IEnumerable<string> args, ArgType type,
        IEnumerable<KeyValuePair<string, string[]>> shortcuts,
        IEnumerable<KeyValuePair<string, string[]>> extraShortcuts)
    {
        IEnumerable<string> StoreMappedShortcut()
        {
            var itrThe = args.GetEnumerator();
            while (itrThe.MoveNext())
            {
                var current = itrThe.Current;
                if (current == TextMapShortcut)
                {
                    if (itrThe.MoveNext())
                    {
                        current = itrThe.Current;
                        foreach (var a2 in current
                            .Split(',')
                            .Select((it) => it.Trim())
                            .Where((it) => it.Length == 3)
                            .Where((it) => (it[1] == '=')
                            && (it[0] != it[2])))
                        {
                            MappedShortcutImp[$"-{a2[0]}"] = $"-{a2[2]}";
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"""
                            {type} Missing value to '{TextMapShortcut}'
                            For example, {TextMapShortcut} s=d
                            """);
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }

        var itrThe = (ArgType.Environment != type)
            ? args.GetEnumerator()
            : StoreMappedShortcut().GetEnumerator();
        IEnumerable<string> SeparateCombiningShortcut()
        {
            while (itrThe.MoveNext())
            {
                var current = itrThe.Current;
                if (current.StartsWith("--"))
                {
                    yield return current;
                }
                else if (current.StartsWith('-') && current.Length > 2)
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

        var mapShortcuts = shortcuts
            .Union(extraShortcuts
            .Select((it) => new KeyValuePair<string, string[]>(
                it.Key, it.Value)))
            .ToImmutableDictionary((it) => it.Key, (it) => it.Value);

        var arg2 = "";
        foreach (var arg in SeparateCombiningShortcut())
        {
            if (arg.Length == 2 && arg[0] == '-')
            {
                arg2 = arg;
                if (MappedShortcutImp.TryGetValue(arg, out var arg3))
                {
                    arg2 = arg3;
                }
                if (mapShortcuts.TryGetValue(arg2, out var expands))
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
            if ((foundAt < 0) || (line.Length <= foundAt)) break;
        }
    }

    static public IEnumerable<int> TextFindAllIndexOf_WordImpl(
        string line, string text)
    {
        bool CheckAlphaNum(int pos, bool isBackward)
        {
            if (isBackward)
            {
                if (pos == 0) return false;
                pos -= 1;
            }
            else
            {
                if (line.Length <= pos) return false;
            }
            return line[pos].IsAlphaNumber();
        }

        int foundAt = 0;
        while ((foundAt = TextIndexOf(line, text, foundAt)) >= 0)
        {
            if ((false == CheckAlphaNum(foundAt, isBackward: true)) &&
                (false == CheckAlphaNum(foundAt + text.Length, false)))
            {
                yield return foundAt;
            }
            foundAt += text.Length;
            if ((foundAt < 0) || (line.Length <= foundAt)) break;
        }
    }

    static public Func<string, string, IEnumerable<int>> TextFindAllIndexOf
    { get; private set; } = TextFindAllIndexOf_Impl;

    static public readonly IInvoke<string, string> PatternWordText
        = new SwitchInvoker<string, string>(TextWord, alterFor: true,
            init: Helper.Itself, alter: (arg) =>
            {
                if (false == arg.StartsWith(@"\b")) arg = @"\b" + arg;
                if (false == arg.EndsWith(@"\b")) arg += @"\b";
                return arg;
            }, alterPost: (flag) =>
            {
                TextFindAllIndexOf = (flag)
                ? TextFindAllIndexOf_WordImpl
                : TextFindAllIndexOf_Impl;
            });

    static public readonly IInvoke<string, RegX.Regex> ToRegex
        = new SwitchInvoker<string, RegX.Regex>(TextCaseSensitive,
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
            TextInvertMatch, alterFor: true,
            init: (matches) => matches.ToArray(),
            alter: (matches) => matches.Any() ? [] : Match.ZeroOne);

    public record PatternParam(bool IsFirstCmdLineArg, string Text);
    static public readonly IInvoke<PatternParam, Pattern> ToPattern =
        new SwitchInvoker<PatternParam, Pattern>(
            TextFixedTextPattern, alterFor: true,
            init: (it) =>
            {
                if (it.IsFirstCmdLineArg)
                {
                    if (it.Text == "~~")
                    {
                        Log.Debug("Pattern of fixed '~~'");
                        return new Pattern("~~");
                    }
                    if (it.Text.StartsWith("~~"))
                    {
                        var a2 = System.Net.WebUtility
                        .UrlDecode(it.Text[2..]);
                        Log.Debug("Pattern of fixed-1st-string '{0}' for '{1}'", a2, it.Text);
                        return new Pattern(a2);
                    }
                    var a3 = ToRegex.Invoke(it.Text);
                    Log.Debug("Pattern of regex-1st-string '{0}' for '{1}'", a3, it.Text);
                    return new Pattern(a3, it.Text);
                }
                var a4 = ToRegex.Invoke(it.Text);
                Log.Debug("Pattern of regex-string '{0}' for '{1}'", a4, it.Text);
                return new Pattern(a4, it.Text);
            },
            alter: (it) =>
            {
                Log.Debug("Pattern of fixed-string '{0}'", it.Text);
                return new Pattern(it.Text);
            });

    const string hintOfFile = "\nRead redir console input if - is assigned to the option.";

    public record PatternsFromResult(bool IsFromRedirConsole,
        Func<string, Match[]> Matches, string[] Args);

    static internal readonly IInvoke<Ignore, IEnumerable<string>> SearchFor = new
        ParseInvoker<Ignore, IEnumerable<string>>(TextSearch, help: "PATTERN",
        init: (_) =>
        {
            Log.Debug("No {0} is assigned", TextSearch);
            return [];
        },
        resolve: (opt, args) =>
        {
            Log.Debug("Assign of {0}", TextSearch);
            opt.SetImplementation((_) => args
            .Select((it) => it.Arg)
            .Where((it) => it.Length > 0));
        });

    /// <summary>
    /// The parsing of 'SearchFor' MUST be called before 'PatternsFrom'.
    /// </summary>
    static public readonly IInvoke<string[], PatternsFromResult>
        PatternsFrom = new ParseInvoker<string[], PatternsFromResult>(
            TextPatternFile, help: "PATTERN-FILE",
            extraHelp: $"For example, {nameof(grep)} {TextPatternFile} regex.txt ..{hintOfFile}",
            init: (args) =>
            {
                var searchFor = SearchFor.Invoke(Ignore.Void)
                .Select((it) => ToPattern.Invoke(new(IsFirstCmdLineArg: false, it)))
                .ToArray();
                Log.Debug("For {2}, Length of {0} is {1}", TextSearch, searchFor.Length, TextPatternFile);
                if (0 < searchFor.Length)
                {
                    return new(false,
                        (string line) => searchFor
                        .Select((it) => it.Matches(line))
                        .FirstOrDefault((it) => it.Length > 0) ?? [],
                        args);
                }

                Pattern pattern;
                switch (args.Length)
                {
                    case 0:
                        throw new ConfigException("No pattern is found.");
                    case 1:
                        pattern = ToPattern.Invoke(new(IsFirstCmdLineArg: true, args[0]));
                        return new(false, (line) => pattern.Matches(line), []);
                    default:
                        pattern = ToPattern.Invoke(new(IsFirstCmdLineArg: true, args[0]));
                        return new(false, (line) => pattern.Matches(line), args.Skip(1).ToArray());
                }
            },
            resolve: (opt, argsThe) =>
            {
                var files = argsThe.Distinct().Take(2).ToArray();
                if (files.Length > 1)
                {
                    ConfigException.TooManyValues(files[0], files[1], opt);
                }

                var fileThe = files[0].Arg;
                if (opt.Help.Equals(fileThe,
                    StringComparison.InvariantCultureIgnoreCase) &&
                    (false == File.Exists(fileThe)))
                {
                    throw new ArgumentException(opt.ExtraHelp);
                }

                var patternFuncs = Helper.ReadAllLinesFromFile(fileThe, opt.Name)
                .Select((it) => it.Trim())
                .Where((it) => it.Length > 0);

                if (false == patternFuncs.Any())
                {
                    throw new ConfigException($"No pattern is found from '{fileThe}'");
                }

                var matchFunc = patternFuncs
                .Union(SearchFor.Invoke(Ignore.Void))
                .Distinct()
                .Select((it) => ToPattern.Invoke(new(false, it)))
                .Select((it) => it.Matches)
                .ToArray();

                opt.SetImplementation((args) =>
                new(Helper.IsShortcutConsoleInput(fileThe),
                (string line) => matchFunc
                .Select((match) => match(line))
                .FirstOrDefault((it) => it.Length > 0) ?? [],
                args));
            });

    public record FilesFromParam(bool IsPatternFromRedirConsole, bool IsArgsEmpty);
    static public readonly IInvoke<FilesFromParam, IEnumerable<string>> FilesFrom
        = new ParseInvoker<FilesFromParam, IEnumerable<string>>(
            TextFilesFrom, help: "FILES-FROM",
            extraHelp: $"For example, {nameof(grep)} {TextFilesFrom} cs-file.txt ..{hintOfFile}",
            init: (arg) =>
             {
                 if (arg.IsPatternFromRedirConsole) return [];
                 if (arg.IsArgsEmpty && Console.IsInputRedirected)
                 {
                     return ["-"];
                 }
                 return [];
             },
            resolve: (opt, argsThe) =>
            {
                var files = argsThe.Distinct().Take(2).ToArray();
                if (files.Length > 1)
                {
                    ConfigException.TooManyValues(files[0], files[1], opt);
                }

                var fileThe = files[0].Arg;
                if (opt.Help.Equals(fileThe,
                    StringComparison.InvariantCultureIgnoreCase) &&
                    (false == File.Exists(fileThe)))
                {
                    throw new ArgumentException(opt.ExtraHelp);
                }

                opt.SetImplementation((_) => Helper.ReadAllLinesFromFile(
                    fileThe, opt.Name)
                .Select((it) => it.Trim())
                .Where((it) => it.Length > 0));
            });

    public static readonly IInvoke<string, string> TrimLine = new
        ParseInvoker<string, string>(TextTrim,
        help: "off | start | end | both", init: Helper.Itself,
        resolve: (opt, argsThe) =>
        {
            var aa = argsThe
            .DistinctBy((it) => it.Arg.ToLower())
            .Take(2)
            .ToArray();
            if (aa.Length > 1)
            {
                ConfigException.TooManyValues(aa[0], aa[1], opt);
                return;
            }
            switch (aa[0].Arg.ToLower())
            {
                case "off":
                    opt.SetImplementation(Helper.Itself);
                    break;
                case "start":
                    opt.SetImplementation((it) => it.TrimStart());
                    break;
                case "end":
                    opt.SetImplementation((it) => it.TrimEnd());
                    break;
                case "both":
                    opt.SetImplementation((it) => it.Trim());
                    break;
                default:
                    ConfigException.WrongValue(aa[0], opt);
                    break;
            }
        });

    public static readonly IInvoke<Ignore, StringComparer>
        FilenameCaseSentive = new SwitchInvoker<Ignore, StringComparer>(
            TextFilenameCaseSensitive, help: HintOnOff, alterFor: true,
            init: (_) => StringComparer.InvariantCultureIgnoreCase,
            alter: (_) => StringComparer.InvariantCulture);

    static IEnumerable<string> AutoSkipArgs(IEnumerable<string> seq)
    {
        var r2 = RegexColotAuto();
        return seq
        .Where((it) => false == r2.IsMatch(it));
    }

    public static readonly IInvoke<IEnumerable<string>, IEnumerable<string>>
        SkipArgs = new ParseInvoker<IEnumerable<string>, IEnumerable<string>>(
            TextSkipArg, help: "auto | off | TEXT",
            init: (seq) => AutoSkipArgs(seq),
            resolve: (opt, argsThe) =>
            {
                var exclTexts = argsThe
                .Select((it) => it.Arg)
                .Where((it) => it.Length > 0)
                .Distinct()
                .ToArray();
                if (exclTexts.Length == 0) return;
                if (exclTexts.Length == 1)
                {
                    Log.Debug("{0}:found '{1}}'", TextSkipArg, exclTexts[0]);
                    switch (exclTexts[0].ToLower())
                    {
                        case "auto":
                            opt.SetImplementation((seq) => AutoSkipArgs(seq));
                            break;
                        case "off":
                            opt.SetImplementation((seq) => seq);
                            break;
                        default:opt.SetImplementation((seq)
                            => seq.Where((it) => false == it.Equals(exclTexts[0],
                            StringComparison.InvariantCulture)));
                            break;
                    }
                    return;
                }
                Log.Debug(exclTexts, "{0}:found", TextSkipArg);
                opt.SetImplementation(
                    (seq) =>
                    seq.Where((it) =>
                    exclTexts.All((it2) => it.Equals(it2,
                    StringComparison.InvariantCultureIgnoreCase))));
            });

    public static readonly IInvoke<IEnumerable<string>, IEnumerable<string>>
        SplitFileByComma = new SwitchInvoker<IEnumerable<string>, IEnumerable<string>>(
            TextSplitFileByComma, help: HintOnOff,
            alterFor: false, alter: Helper.Itself,
            init: (seq) => seq
            .Select((it) => it.Split(','))
            .SelectMany((it) => it)
            .Where((it) => 0 < it.Length)
            .Distinct());

    public static readonly IParse[] Parsers = [
        (IParse)ToRegex,
        (IParse)Show.Filename,
        (IParse)Show.LineNumber,
        (IParse)Show.LogVerbose,
        (IParse)PatternWordText,
        (IParse)ToPattern,
        (IParse)Show.PauseMaker,
        (IParse)Show.TakeSumByMax,
        (IParse)Show.PrintLineMaker,
        (IParse)SubDir.ExclFile,
        (IParse)SubDir.ExclDir,
        (IParse)Show.PrintTotal,
        (IParse)TrimLine,
        (IParse)FilenameCaseSentive,
        (IParse)SkipArgs,
        (IParse)SplitFileByComma,
        (IParse)Show.MaxReportFileNotFound,
    ];

    // The position of 'PatternsFrom' MUST be prior to 'FilesFrom'
    public static readonly IParse[] NonEnvirParsers = [
        (IParse)SearchFor,
        (IParse)PatternsFrom,
        (IParse)FilesFrom,
        (IParse)MetaMatches,
        (IParse)Show.MatchedFilenameWithCount,
        (IParse)Show.MatchedFilenameOnly,
        (IParse)SubDir.FileScan,
    ];

    public static readonly IParse[] ParsersForShortHelp = [
        (IParse)ToRegex,
        (IParse)PatternWordText,
        (IParse)Show.LineNumber,
        (IParse)Show.TakeSumByMax,
        (IParse)Show.PrintLineMaker,
        (IParse)SubDir.ExclFile,
        (IParse)SubDir.ExclDir,
    ];

    public static readonly IParse[] NonEnvirParsersForShortHelp = [
        (IParse)MetaMatches,
        (IParse)Show.MatchedFilenameWithCount,
        (IParse)Show.MatchedFilenameOnly,
        (IParse)SubDir.FileScan,
    ];

    static public IEnumerable<FlagedArg> Resolve(this IEnumerable<FlagedArg> args,
        IEnumerable<IParse> extraParsers)
    {
        return Parsers
            .Union(extraParsers)
            .Aggregate(seed: args, func: (acc, it) => it.Parse(acc));
    }

    [RegX.GeneratedRegex(@"\-\-color.*=.*\bauto")]
    private static partial RegX.Regex RegexColotAuto();
}
