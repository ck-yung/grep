using System.Collections.Immutable;
using System.Diagnostics;
using static grep.MyOptions;
using RegX = System.Text.RegularExpressions;

namespace grep;

internal static class Options
{
    public const string TextOff = "off";
    public const string TextOn = "on";

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
        ];

    public static readonly IEnumerable<KeyValuePair<string, string[]>>
        ShortCuts =
        [
            new("-F", [TextFixedTextPattern, TextOn]),
            new("-h", [TextShowFilename, TextOff]),
            new("-i", [TextCaseSensitive, TextOff]),
            new("-n", [TextLineNumber, TextOn]),
            new("-p", [TextPause, TextOff]),
            new("-q", [TextQuiet, TextOn]),
            new("-w", [TextWord, TextOn]),
            new("-x", [TextExclFile]),
            new("-X", [TextExclDir]),
        ];

    public static IEnumerable<FlagedArg> ToFlagedArgs(
        this IEnumerable<string> args, ArgType type,
        IEnumerable<KeyValuePair<string, string[]>> shortcuts,
        IEnumerable<KeyValuePair<string, string[]>> extraShortcuts)
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
                        Log.Debug($"Pattern of fixed-1st-string '{a2}' for '{it.Text}'");
                        return new Pattern(a2);
                    }
                    var a3 = ToRegex.Invoke(it.Text);
                    Log.Debug($"Pattern of regex-1st-string '{a3}' for '{it.Text}'");
                    return new Pattern(a3);
                }
                var a4 = ToRegex.Invoke(it.Text);
                Log.Debug($"Pattern of regex-string '{a4}' for '{it.Text}'");
                return new Pattern(a4);
            },
            alter: (it) =>
            {
                Log.Debug($"Pattern of fixed-string '{it.Text}'");
                return new Pattern(it.Text);
            });

    const string hintOfFile = "\nRead redir console input if - is assigned to the option.";

    public record PatternsFromResult(bool IsFromRedirConsole,
        Func<string, Match[]> Matches, string[] Args);

    static public readonly IInvoke<string[], PatternsFromResult>
        PatternsFrom = new ParseInvoker<string[], PatternsFromResult>(
            TextPatternFile, help: "PATTERN-FILE",
            extraHelp: $"For example, {nameof(grep)} {TextPatternFile} regex.txt ..{hintOfFile}",
            init: (args) =>
            {
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
                    throw new ConfigException(
                        $"Too many files ('{files[0].Arg}','{files[1].Arg}') to {opt.Name} are found!");
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
                .Where((it) => it.Length > 0)
                .Distinct()
                .Select((it) => ToPattern.Invoke(new(false, it)))
                .Select((it) => it.Matches)
                .ToArray();

                Match[] matchFunc(string line) => patternFuncs
                .Select((matches) => matches(line))
                .FirstOrDefault((it) => it.Length > 0)
                ?? [];

                opt.SetImplementation((args) => new("-" == fileThe, matchFunc, args));
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
                    throw new ConfigException(
                        $"Too many files ('{files[0].Arg}','{files[1].Arg}') to {opt.Name} are found!");
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

    public static readonly IParse[] Parsers = [
        (IParse)ToRegex,
        (IParse)Show.Filename,
        (IParse)Show.LineNumber,
        (IParse)Show.LogVerbose,
        (IParse)PatternWordText,
        (IParse)ToPattern,
        (IParse)Show.PauseMaker,
        (IParse)Show.MaxFound,
        (IParse)Show.PrintLineMaker,
        (IParse)Show.AddFoundCount,
        (IParse)SubDir.ExclFile,
        (IParse)SubDir.ExclDir,
    ];

    // The position of 'PatternsFrom' MUST be prior to 'FilesFrom'
    public static readonly IParse[] NonEnvirParsers = [
        (IParse)PatternsFrom,
        (IParse)FilesFrom,
        (IParse)MetaMatches,
        (IParse)Show.FilenameOnly,
        (IParse)Show.SummaryMaker,
        (IParse)SubDir.FileScan,
    ];

    static public IEnumerable<FlagedArg> Resolve(this IEnumerable<FlagedArg> args,
        IEnumerable<IParse> extraParsers)
    {
        return Parsers
            .Union(extraParsers)
            .Aggregate(seed: args, func: (acc, it) => it.Parse(acc));
    }
}
