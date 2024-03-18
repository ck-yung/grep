using static grep.MyOptions;
using static grep.Options;

namespace grep;

internal static partial class Show
{
    static public readonly IInvoke<string, int> Filename =
        new SwitchInvoker<string, int>(TextShowFilename,
            init: (filename) =>
            {
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

    #region File Count and Found Count
    static int TotalFileCountImp { get; set; } = 0;
    static int TotalFoundCountImp { get; set; } = 0;
    static public readonly IInvoke<Else<int, Ignore>, Ignore> TotalCount =
        new SwitchInvoker<Else<int, Ignore>, Ignore>(TextTotal,
            help: TextOn, alterFor: true,
            init: (_) => Ignore.Void,
            alter: (it) =>
            {
                if (it.IsRight)
                {
                    if (it.Right() > 0)
                    {
                        TotalFileCountImp += 1;
                        TotalFoundCountImp += it.Right();
                    }
                }
                else
                {
                    var tmp = (TotalFileCountImp, TotalFoundCountImp) switch
                    {
                        (0, 0) => "No finding is matched.",
                        (1, 1) => "One finding is matched.",
                        (1, _) => $"{TotalFoundCountImp} findings in a file are matched.",
                        _ => $"{TotalFoundCountImp} findings in {TotalFileCountImp} files are matched.",
                    };
                    Console.WriteLine(tmp);
                }
                return Ignore.Void;
            });
    #endregion

    public record CountFound(IConsolePause Pause, string Filename, int Count);

    static public readonly IInvoke<CountFound, bool> FoundCount =
        new SwitchInvoker<CountFound, bool>(TextCountOnly,
            init: (it) => it.Count > 0,
            alterFor: true, alterPost: (flag) =>
            {
                if (true == flag)
                {
                    ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                    ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                    ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);
                }
            },
            alter: (it) =>
            {
                if (it.Count > 0)
                {
                    var msg = $"{it.Filename}:{it.Count}";
                    Console.WriteLine(msg);
                    it.Pause.Printed(msg.Length);
                    return true;
                }
                return false;
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
        IEnumerable<MatchResult>, IEnumerable<MatchResult>>
        MaxFound = new ParseInvoker<
            IEnumerable<MatchResult>, IEnumerable<MatchResult>>(
            name: TextMaxCount, help: "NUMBER", init: Helper.Itself,
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

                if (int.TryParse(args[0].Arg, out var takeCount))
                {
                    if (takeCount > 0)
                    {
                        opt.SetImplementation((seq) => seq.Take(takeCount));
                    }
                    else
                    {
                        throw new ConfigException(
                            $"Value to {opt.Name} SHOULD be greater than zero but {takeCount} is found!");
                    }
                }
                else
                {
                    throw new ConfigException(
                        $"Value to {opt.Name} SHOULD be a number but '{args[0].Arg}' is found!");
                }
            });

    static public readonly IInvoke<Ignore, Ignore> FilenameOnly =
        new SwitchInvoker<Ignore, Ignore>(TextFileMatch,
            init: Ignore.Maker, alter: Ignore.Maker,
            alterFor: true, alterPost: (flag) =>
            {
                if (true == flag)
                {
                    ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                    ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                    ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);

                    ((SwitchInvoker<CountFound, bool>)FoundCount)
                    .SetImplementation((it) =>
                    {
                        if (it.Count > 0)
                        {
                            Console.WriteLine(it.Filename);
                            it.Pause.Printed(it.Filename.Length);
                            return true;
                        }
                        return false;
                    });
                }
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
}
