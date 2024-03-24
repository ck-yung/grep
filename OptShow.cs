using static grep.MyOptions;
using static grep.Options;
namespace grep;

internal static partial class Show
{
    static public readonly IInvoke<string, int> Filename =
        new SwitchInvoker<string, int>(TextShowFilename,
            init: (filename) =>
            {
                if (Helper.IsShortcutConsoleInput(filename)) return 0;
                var msg = $"{filename}:";
                Console.Write(msg);
                return msg.Length;
            },
            alterFor: false, alter: (_) => 0);

    /// <summary>
    /// Invoke(string) return
    /// true, if continue
    /// false, if break for scanning the file
    /// </summary>
    static public readonly IInvoke<int, int> LineNumber =
        new SwitchInvoker<int, int>(TextLineNumber,
            init: (_) => 0,
            alterFor: true, alter: (lineNumber) =>
            {
                var msg = $"{1 + lineNumber}:";
                Console.Write(msg);
                return msg.Length;
            });

    static public readonly IInvoke<string, Ignore> LogVerbose =
        new SwitchInvoker<string, Ignore>(TextQuiet,
            init: (msg) =>
            {
                if (false == string.IsNullOrEmpty(msg))
                {
                    Console.WriteLine(msg);
                }
                return Ignore.Void;
            },
            alterFor: true, alter: Ignore<string>.Maker);

    static public readonly IInvoke<IEnumerable<int>, int>
        TakeSumByMax = new ParseInvoker<IEnumerable<int>, int>(
            name: TextMaxCount, help: "NUMBER",
            init: (seq) => seq.Sum(),
            extraHelp: $"For example, {nameof(grep)} {TextMaxCount} 2 ..",
            resolve: (opt, argsThe) =>
            {
                var args = argsThe.Distinct().Take(2).ToArray();
                if (args.Length > 1)
                {
                    throw new ConfigException(
                        $"Too many values ({args[0].Arg},{args[1].Arg}) to {opt.Name}");
                }

                if (opt.Help.Equals(args[0].Arg,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ArgumentException(opt.ExtraHelp);
                }

                if (false == int.TryParse(args[0].Arg, out int maxSumThe))
                    throw new ConfigException(
                        $"Value to {opt.Name} SHOULD be a number but '{args[0].Arg}' is found!");

                if (1 > maxSumThe)
                    throw new ConfigException(
                        $"Value to {opt.Name} SHOULD be greater than zero but {maxSumThe} is found!");
                Log.Debug("{0} := {1}", TextMaxCount, maxSumThe);
                opt.SetImplementation((seq) =>
                {
                    int sum = 0;
                    var itr = seq.GetEnumerator();
                    while (itr.MoveNext())
                    {
                        sum += itr.Current;
                        if (sum >= maxSumThe) break;
                    }
                    return sum;
                });
            });

    static public readonly IInvoke<Ignore, IConsolePause> PauseMaker =
        new SwitchInvoker<Ignore, IConsolePause>(TextPause,
            init: (_) =>
            {
                if (Console.IsInputRedirected || Console.IsOutputRedirected)
                {
                    return new FakePause();
                }
                return new ConsolePause();
            },
            alterFor: false,
            alter: (_) => new FakePause());

    public class InfoTotal(IConsolePause pause)
    {
        public int Count { get; private set; } = 0;
        public int AddCount { get; private set; } = 0;
        public int MatchedFileCount { get; private set; } = 0;
        public InfoTotal AddWith((string Path, int Count) arg)
        {
            AddCount++;
            if (arg.Count > 0)
            {
                MatchedFileCount++;
                Count += arg.Count;
                var lenPrinted = FileFinding.PrintInfo(arg);
                pause.Printed(lenPrinted);
            }
            return this;
        }
    }

    static bool IsNoneMatched(InfoTotal total)
    {
        if (0 == total.AddCount)
        {
            LogVerbose.Invoke("No file is found.");
            return true;
        }
        if (0 == total.MatchedFileCount)
        {
            LogVerbose.Invoke($"No finding is matched for '{Pattern.First}'.");
            return true;
        }
        return false;
    }
    static Ignore PrintTotalWithFindingCount(InfoTotal total)
    {
        if (IsNoneMatched(total)) return Ignore.Void;
        switch (total.MatchedFileCount, total.Count)
        {
            case (1, 1):
                Console.WriteLine("Only a finding in a file is matched.");
                break;
            case (1, _):
                Console.WriteLine($"{total.Count} findings in a file are matched.");
                break;
            default:
                Console.WriteLine(
                    $"{total.Count} findings in {total.MatchedFileCount} files are matched.");
                break;
        }
        return Ignore.Void;
    }

    static Ignore PrintTotalWithoutFindingCount(InfoTotal total)
    {
        if (IsNoneMatched(total)) return Ignore.Void;
        switch (total.MatchedFileCount)
        {
            case 1:
                Console.WriteLine("Only a file is matched.");
                break;
            default:
                Console.WriteLine(
                    $"{total.MatchedFileCount} files are matched.");
                break;
        }
        return Ignore.Void;
    }

    static Func<InfoTotal, Ignore> PrintTotalRelatedWithCount
    { get; set; } = PrintTotalWithFindingCount;

    static class FileFinding
    {
        public static string Option { get; set; } = "";
        public static Func<(string Path, int Count), int> PrintInfo = (_) => 0;
        public static void Assign(string option,
            Func<(string Path, int Count), int> printInfo)
        {
            if (false == string.IsNullOrEmpty(Option))
            {
                throw new ArgumentException(
                    $"Options '{Option} on' and '{option}' can NOT both be set.");
            }
            Option = option;
            PrintInfo = printInfo;
        }
    }

    static public readonly IInvoke<Ignore, Ignore> MatchedFilenameOnly =
        new SwitchInvoker<Ignore, Ignore>(TextFileMatch, alterFor: true,
            init: Ignore.Maker, alter: (_) =>
            {
                return Ignore.Void;
            },
            alterPre: () =>
            {
                FileFinding.Assign(TextFileMatch + " on", printInfo: (it) =>
                {
                    Console.WriteLine(it.Path);
                    return it.Path.Length;
                });
                PrintTotalRelatedWithCount = (it) => PrintTotalWithoutFindingCount(it);
            },
            alterPost: (flag) =>
            {
                if (flag)
                {
                    ((IParse)TakeSumByMax).Parse("1".ToFlagedArgs());
                    ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                    ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                    ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);
                }
            });

    static public readonly IInvoke<Ignore, Ignore> MatchedFilenameWithCount =
        new SwitchInvoker<Ignore, Ignore>(TextCountOnly, alterFor: true,
            init: Ignore.Maker, alter: (_) =>
            {
                return Ignore.Void;
            },
            alterPre: () =>
            {
                if (false == string.IsNullOrEmpty(FileFinding.Option))
                {
                    throw new ArgumentException(
                        $"Options '{TextCountOnly} on' and '{FileFinding.Option}' can NOT both be set.");
                }
                FileFinding.Assign(TextCountOnly + " on", printInfo: (it) =>
                {
                    var msg = $"{it.Count}\t{it.Path}";
                    Console.WriteLine(msg);
                    return msg.Length;
                });
            },
            alterPost: (flag) =>
            {
                if (flag)
                {
                    ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                    ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                    ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);
                }
            });

    static public readonly IInvoke<InfoTotal, Ignore> PrintTotal =
        new ParseInvoker<InfoTotal, Ignore>(
            TextTotal, help: "on | off | only",
            init: (total) =>
            {
                IsNoneMatched(total);
                return Ignore.Void;
            },
            resolve: (opt, argsThe) =>
            {
                var aa = argsThe
                .Where((it) => it.Arg.Length > 0)
                .Distinct()
                .Take(2)
                .ToArray();
                if (aa.Length > 1)
                {
                    ConfigException.Add(aa[0].Type,
                        new ConfigException(
                            $"Too many values ('{aa[0].Arg}','{aa[1].Arg}') to {opt.Name}"));
                    return;
                }
                switch (aa[0].Arg.ToLower())
                {
                    case TextOff:
                        opt.SetImplementation((_) => Ignore.Void);
                        break;
                    case TextOn:
                        opt.SetImplementation((it) => PrintTotalRelatedWithCount(it));
                        break;
                    case "only":
                        var typeThe = aa[0].Type;
                        if (typeThe != ArgType.CommandLine)
                        {
                            ConfigException.Add(typeThe,
                                new ConfigException(
                                    $"'{aa[0].Arg}' is ignored to {opt.Name}"));
                            return;
                        }
                        FileFinding.Assign(TextTotal + " only", printInfo: (_) => 0);
                        ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                        ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                        ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);
                        opt.SetImplementation((it) => PrintTotalWithFindingCount(it));
                        break;
                    default:
                        ConfigException.Add(aa[0].Type,
                            new ConfigException(
                                $"Value '{aa[0].Arg}' is unknown to {opt.Name}"));
                        break;
                }
            });
}
