using static grep.MyOptions;
using static grep.Options;

namespace grep;

internal static partial class Show
{
    static public readonly IInvoke<string, int> Filename =
        new SwitchInvoker<string, int>(TextShowFilename,
            init: (filename) =>
            {
                if ("-" == filename) return 0;
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

    static public readonly IInvoke<
        IEnumerable<int>, int>
        TakeSumByMax = new ParseInvoker<
            IEnumerable<int>, int>(
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

    public class PathFindingParam(string path, int count,
        IConsolePause pause)
    {
        public string Path { get; init; } = path;
        public int Count { get; private set; } = count;
        public IConsolePause Pause { get; init; } = pause;
        public int AddCount { get; private set; } = 0;
        public int FileCount { get; private set; } = 0;
        public PathFindingParam AddWith(PathFindingParam other)
        {
            AddCount++;
            if (other.Count > 0)
            {
                FileCount++;
                Count += other.Count;
            }
            return this;
        }
    }

    public interface IPrintPathCount
    {
        PathFindingParam Print(PathFindingParam arg);
        Ignore TotalPrint(Else<PathFindingParam, int> arg);
    }

    class PrintPathWithCount(bool isPrintFinding) : IPrintPathCount
    {
        readonly bool IsPrintFinding = isPrintFinding;
        public PathFindingParam Print(PathFindingParam arg)
        {
            if (arg.Count > 0)
            {
                if (true == IsPrintFinding)
                {
                    var msg = $"{arg.Count}\t{arg.Path}";
                    Console.WriteLine(msg);
                    arg.Pause.Printed(msg.Length);
                }
            }
            return arg;
        }

        public Ignore TotalPrint(Else<PathFindingParam, int> arg)
        {
            if (true != arg.IsFirst)
            {
                if (0 == arg.Second())
                {
                    LogVerbose.Invoke("No file is found.");
               }
                return Ignore.Void;
            }

            var total = arg.First();
            if (total.AddCount == 0)
            {
                LogVerbose.Invoke("No file is found.");
                return Ignore.Void;
            }
            switch (total.FileCount, total.Count)
            {
                case (0, 0):
                    Console.WriteLine("No finding is matched.");
                    break;
                case (1, 1):
                    Console.WriteLine("Only a finding in a file is matched.");
                    break;
                case (1, _):
                    Console.WriteLine($"{total.Count} findings in a file are matched.");
                    break;
                default:
                    Console.WriteLine(
                        $"{total.Count} findings in {total.FileCount} files are matched.");
                    break;
            }
            return Ignore.Void;
        }
    }

    class PrintPathWithoutCount : IPrintPathCount
    {
        public PathFindingParam Print(PathFindingParam arg)
        {
            if (arg.Count > 0)
            {
                Console.WriteLine(arg.Path);
                arg.Pause.Printed(arg.Path.Length);
                Log.Debug($"new> FindingResult(sum:{0}) [2]", arg.Count);
            }
            return arg;
        }

        public Ignore TotalPrint(Else<PathFindingParam, int> arg)
        {
            if (true != arg.IsFirst)
            {
                if (0 == arg.Second())
                {
                    LogVerbose.Invoke("No file is found.");
                }
                return Ignore.Void;
            }

            var total = arg.First();
            if (total.AddCount == 0)
            {
                LogVerbose.Invoke("No file is found.");
                return Ignore.Void;
            }
            switch (total.FileCount)
            {
                case (0):
                    Console.WriteLine("No finding is matched.");
                    break;
                case (1):
                    Console.WriteLine("A file is matched.");
                    break;
                default:
                    Console.WriteLine(
                        $"{total.FileCount} files are matched.");
                    break;
            }
            return Ignore.Void;
        }
    }

    static public readonly IInvoke<bool, IPrintPathCount>
        MatchedFilenameOnly = new SwitchInvoker<bool, IPrintPathCount>(
            TextFileMatch, alterFor: true,
            init: (flag) => new PrintPathWithCount(flag),
            alter: (flag) => new PrintPathWithoutCount(),
            alterPost: (flag) =>
            {
                ((IParse)TakeSumByMax).Parse("1".ToFlagedArgs());
                ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);
            });

    static public readonly IInvoke<Ignore, IPrintPathCount>
        PrintMaker = new SwitchInvoker<Ignore, IPrintPathCount>(
            TextCountOnly,
            alterFor: true,
            init: (_) => MatchedFilenameOnly.Invoke(false),
            alter: (_) => MatchedFilenameOnly.Invoke(true),
            alterPost: (flag) =>
            {
                ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);
            });

    static public readonly IInvoke<PathFindingParam, Else<PathFindingParam, int>>
        PrintTotal = new
        SwitchInvoker<PathFindingParam, Else<PathFindingParam, int>>(
            TextTotal, init: (it) => new Else<PathFindingParam, int>(it.AddCount),
            alterFor: true, alter: (it) => new Else<PathFindingParam, int>(it));
}
