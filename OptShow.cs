using static grep.MyOptions;
using static grep.Options;
namespace grep;

internal static partial class Show
{
    public abstract class FilenameAction
    {
        protected readonly IConsolePause Pause;
        public FilenameAction(IConsolePause pause)
        {
            Pause = pause;
        }
        public abstract void Assign(string filename);
        public abstract int Print();
        public abstract void PrintFooter();
    }

    public class NeverPrintFilename : FilenameAction
    {
        public NeverPrintFilename(IConsolePause pause)
            : base(pause) { }
        public override void Assign(string filename) { }
        public override int Print() => 0;
        public override void PrintFooter() { }
    }

    public class AlwaysPrintFilename: FilenameAction
    {
        public AlwaysPrintFilename(IConsolePause pause)
            : base(pause) { }
        protected string Current = "?";
        protected Func<int> PrintImpl = () => 0;
        protected int RealPrintImpl()
        {
            var msg = $"{Current}:";
            Console.Write(msg);
            return msg.Length;
        }
        public override void Assign(string filename)
        {
            //Log.Debug("AlwaysPrintFilename.Assign('{0}')", filename);
            Current = filename;
            PrintImpl = (Helper.IsShortcutConsoleInput(Current))
                ? Always.Zero : RealPrintImpl;
        }
        public override int Print() => PrintImpl();
        public override void PrintFooter() { }
    }

    public class SmartPrintFilename: AlwaysPrintFilename
    {
        public SmartPrintFilename(IConsolePause pause)
            : base(pause) { }
        public override void Assign(string filename)
        {
            Current = filename;
            PrintFooterImpl = Always.DoNothing;
            if (Helper.IsShortcutConsoleInput(Current))
            {
                PrintImpl = Always.Zero;
            }
            else
            {
                PrintImpl = () =>
                {
                    var msg = $"> {Current}";
                    Console.WriteLine(msg);
                    Pause.Printed(msg.Length);
                    PrintImpl = Always.Zero;
                    PrintFooterImpl = RealPrintFooterImpl;
                    return 0;
                };
            }
        }
        public override int Print()
        {
            return base.Print();
        }
        Action PrintFooterImpl = Always.DoNothing;
        void RealPrintFooterImpl()
        {
            if (Helper.IsShortcutConsoleInput(Current)) return;
            var msg = $"< {Current}";
            Console.WriteLine(msg);
            Pause.Printed(msg.Length);
        }
        public override void PrintFooter() => PrintFooterImpl();
    }

    static public readonly IInvoke<IConsolePause, FilenameAction> Filename =
        new ParseInvoker<IConsolePause, FilenameAction>(TextShowFilename,
            help: "on | off | smart",
            init: (arg) => new AlwaysPrintFilename(arg),
            resolve: (opt, argsThe) =>
            {
                var aa = argsThe
                .Where((it) => 0 < it.Arg.Length)
                .DistinctBy((it) => it.Arg)
                .Take(2)
                .ToArray();
                if (1 < aa.Length)
                {
                    ConfigException.TooManyValues(aa[0], aa[1], opt);
                    return;
                }
                switch (aa[0].Arg)
                {
                    case TextOff:
                        opt.SetImplementation((arg) => new NeverPrintFilename(arg));
                        break;
                    case TextOn:
                        opt.SetImplementation((arg) => new AlwaysPrintFilename(arg));
                        break;
                    case "smart":
                        opt.SetImplementation((arg) => new SmartPrintFilename(arg));
                        break;
                    default:
                        ConfigException.WrongValue(aa[0], opt);
                        break;
                }
            });

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
                    ConfigException.TooManyValues(args[0], args[1], opt);
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
                    return FakePause.Null;
                }
                return new ConsolePause();
            },
            alterFor: false,
            alter: (_) => FakePause.Null);

    public class InfoTotal
    {
        public int Count { get; private set; } = 0;
        public int AddCount { get; private set; } = 0;
        public int MatchedFileCount { get; private set; } = 0;
        public InfoTotal AddWith(FileFinding arg)
        {
            AddCount++;
            if (arg.Count > 0)
            {
                MatchedFileCount++;
                Count += arg.Count;
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
                Console.WriteLine("Total: Only a finding is matched.");
                break;
            case (1, _):
                Console.WriteLine($"Total: {total.Count} findings in a file are matched.");
                break;
            default:
                Console.WriteLine(
                    $"Total: {total.Count} findings in {total.MatchedFileCount} files are matched.");
                break;
        }
        return Ignore.Void;
    }

    static Ignore PrintTotalWithoutFindingCount(InfoTotal total)
    {
        if (IsNoneMatched(total)) return Ignore.Void;
        if (1 == total.MatchedFileCount)
        {
            Console.WriteLine("Total: Only a file is matched.");
        }
        else
        {
            Console.WriteLine(
                $"Total: {total.MatchedFileCount} files are matched.");

        }
        return Ignore.Void;
    }

    static Func<InfoTotal, Ignore> PrintTotalRelatedWithCount
    { get; set; } = PrintTotalWithFindingCount;

    internal record FileFinding(string Path, int Count);

    public static class FindingReport
    {
        static IConsolePause Pause { get; set; } = FakePause.Null;
        static Action<string, int, IConsolePause> Prepare = (_, _, _) =>{};
        public static FileFinding Make(string path, int count)
        {
            Prepare(path, count, Pause);
            return new(path, count);
        }

        public static string AssignedBy { get; private set; } = "";
        public static IConsolePause Assign(string by, IConsolePause pause,
            Action<string, int, IConsolePause> prepare)
        {
            if (false == string.IsNullOrEmpty(AssignedBy))
            {
                throw new ArgumentException(
                    $"Options '{AssignedBy}' and '{by}' can NOT both be set.");
            }
            AssignedBy = by;
            Pause = pause;
            Prepare = prepare;
            return pause;
        }
    }

    static public readonly IInvoke<IConsolePause, IConsolePause> MatchedFilenameOnly =
        new SwitchInvoker<IConsolePause, IConsolePause>(TextFileMatch, alterFor: true,
            init: Helper.Itself, alter: (it) =>
            {
                ((IParse)TakeSumByMax).Parse("1".ToFlagedArgs());
                ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);
                PrintTotalRelatedWithCount =
                (it) => PrintTotalWithoutFindingCount(it);
                return FindingReport.Assign(TextFileMatch + " on",
                    pause: it, prepare: (path, count, pause) =>
                    {
                        if (0 < count)
                        {
                            Console.WriteLine(path);
                            pause.Printed(path.Length);
                        }
                    });
            });

    static public readonly IInvoke<IConsolePause, IConsolePause> MatchedFilenameWithCount =
        new SwitchInvoker<IConsolePause, IConsolePause>(TextCountOnly, alterFor: true,
            init: Helper.Itself, alter: (it) =>
            {
                ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);
                PrintTotalRelatedWithCount =
                (it) => PrintTotalWithFindingCount(it);
                return FindingReport.Assign(TextCountOnly + " on",
                    pause: it, prepare: (path, count, pause) =>
                    {
                        var msg = $"{count}\t{path}";
                        Console.WriteLine(msg);
                        pause.Printed(msg.Length);
                    });
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
                    ConfigException.TooManyValues(aa[0], aa[1], opt);
                    return;
                }
                var typeThe = aa[0].Type;
                switch (aa[0].Arg.ToLower())
                {
                    case TextOff:
                        opt.SetImplementation((_) => Ignore.Void);
                        break;
                    case TextOn:
                        opt.SetImplementation((it) => PrintTotalRelatedWithCount(it));
                        break;
                    case "only":
                        if (typeThe != ArgType.CommandLine)
                        {
                            ConfigException.Add(typeThe,
                                $"'{aa[0].Arg}' is ignored to {opt.Name}");
                            return;
                        }
                        FindingReport.Assign(TextTotal + " only",
                            pause: FakePause.Null, prepare: (path, count, pause) => { });
                        ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                        ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                        ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);
                        opt.SetImplementation((it) => PrintTotalWithFindingCount(it));
                        break;
                    default:
                        ConfigException.WrongValue(aa[0], opt);
                        break;
                }
            });

    public static int CountReportFileNotFound = 3;
    public static int CountReportFileNotMatched = 3;
    static public readonly IInvoke<Ignore, Ignore> MaxReportFileNotFound =
        new ParseInvoker<Ignore, Ignore>(
            TextMaxReportFileNotFound, help: "NUMBER | off",
            init: Ignore.Maker,
            resolve: (opt, argsThe) =>
            {
                var aa = argsThe
                .Where((it) => it.Arg.Length > 0)
                .Distinct()
                .Take(2)
                .ToArray();
                if (aa.Length > 1)
                {
                    ConfigException.TooManyValues(aa[0], aa[1], opt);
                    return;
                }
                if (TextOff.Equals(aa[0].Arg, StringComparison.InvariantCultureIgnoreCase))
                {
                    CountReportFileNotFound = int.MaxValue;
                    CountReportFileNotMatched = int.MaxValue;
                }
                else if (int.TryParse(aa[0].Arg, out var maxThe))
                {
                    CountReportFileNotFound = maxThe;
                    CountReportFileNotMatched = maxThe;
                }
                else
                {
                    ConfigException.WrongValue(aa[0], opt);
                }
            });
}
