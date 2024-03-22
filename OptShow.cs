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
    }

    public class TotalPathFindingParam
    {
        public int Count { get; private set; } = 0;
        public int AddCount { get; private set; } = 0;
        public int FileCount { get; private set; } = 0;
        public TotalPathFindingParam AddWith(PathFindingParam other)
        {
            AddCount++;
            if (other.Count > 0)
            {
                FileCount++;
                Count += other.Count;
                FileFinding.PrintFile(other);
            }
            return this;
        }
    }

    static Ignore PrintTotalWithFindingCount(TotalPathFindingParam total)
    {
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

    static Ignore PrintTotalWithoutFindingCount(TotalPathFindingParam total)
    {
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
                Console.WriteLine("Only a file is matched.");
                break;
            default:
                Console.WriteLine(
                    $"{total.FileCount} files are matched.");
                break;
        }
        return Ignore.Void;
    }

    static Func<TotalPathFindingParam, Ignore> PrintTotalImpl { get; set; }
    = PrintTotalWithFindingCount;

    record FileFindingFeature(string Option,
        Func<PathFindingParam, PathFindingParam> PrintFile);

    static FileFindingFeature FileFinding { get; set; } =
        new("", Helper.Itself<PathFindingParam>);

    static public readonly IInvoke<Ignore, Ignore> MatchedFilenameOnly =
        new SwitchInvoker<Ignore, Ignore>(TextFileMatch, alterFor: true,
            init: Ignore.Maker, alter: (_) =>
            {
                return Ignore.Void;
            },
            alterPre: () =>
            {
                if (false == string.IsNullOrEmpty(FileFinding.Option))
                {
                    throw new ArgumentException(
                        $"Options '{TextFileMatch} on' and '{FileFinding.Option}' can NOT both be set.");
                }
                FileFinding = new(TextFileMatch + " on", (it) =>
                {
                    Console.WriteLine(it.Path);
                    it.Pause.Printed(it.Path.Length);
                    return it;
                });
                PrintTotalImpl = (it) => PrintTotalWithoutFindingCount(it);
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
                FileFinding = new(TextCountOnly + " on", (it) =>
                {
                    var msg = $"{it.Count}\t{it.Path}";
                    Console.WriteLine(msg);
                    it.Pause.Printed(msg.Length);
                    return it;
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

    static public readonly IInvoke<TotalPathFindingParam, Ignore> PrintTotal =
        new ParseInvoker<TotalPathFindingParam, Ignore>(
            TextTotal, help: "on | off | only",
            init: (it) => Ignore.Void,
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
                        opt.SetImplementation((it) => PrintTotalImpl(it));
                        break;
                    case "only":
                        var typeThe = aa[0].Type;
                        if (typeThe == ArgType.CommandLine)
                        {
                            if (false == string.IsNullOrEmpty(FileFinding.Option))
                            {
                                throw new ArgumentException(
                                    $"Options '{TextTotal} only' and '{FileFinding.Option}' can NOT both be set.");
                            }
                            ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                            ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                            ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);
                            FileFinding = new(TextTotal + " only", Helper.Itself);
                            opt.SetImplementation((it) => PrintTotalWithFindingCount(it));
                        }
                        else
                        {
                            ConfigException.Add(typeThe,
                                new ConfigException(
                                    $"'{aa[0].Arg}' is ignored to {opt.Name}"));
                        }
                        break;
                    default:
                        ConfigException.Add(aa[0].Type,
                            new ConfigException(
                                $"Value '{aa[0].Arg}' is unknown to {opt.Name}"));
                        break;
                }
            });
}
