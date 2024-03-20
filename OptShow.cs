﻿using static grep.MyOptions;
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

    #region File Count and Found Count
    static int GrandTotalFileCount { get; set; } = 0;
    static int GrandTotalFindingCount { get; set; } = 0;

    /// <summary>
    /// Add finding counter by passing 'int', OR,
    /// print total counter by passing 'Ignore.Void'
    /// </summary>
    static public readonly IInvoke<Else<int, Ignore>, Ignore> AddFoundCount =
        new SwitchInvoker<Else<int, Ignore>, Ignore>(TextTotal,
            help: TextOn, alterFor: true,
            init: (it) => Ignore.Void,
            alter: (it) =>
            {
                if (it.IsRight)
                {
                    if (it.Right() != 0)
                    {
                        GrandTotalFileCount += 1;
                        GrandTotalFindingCount += it.Right();
                    }
                    return Ignore.Void;
                }

                Log.Debug("GRAND: #find:{0}; #file:{1}",
                    GrandTotalFindingCount, GrandTotalFileCount);
                var tmp = (GrandTotalFindingCount) switch
                {
                    0 => (GrandTotalFileCount) switch
                    {
                        0 => "No file is matched.",
                        _ => $"#file:{GrandTotalFileCount}, nothing is matched.",
                    },
                    (int cnt) when (cnt < 0) => (GrandTotalFileCount) switch
                    {
                        1 => "A file is matched.",
                        _ => $"{GrandTotalFileCount} files are matched.",
                    },
                    _ => (GrandTotalFileCount) switch
                    {
                        1 => $"{GrandTotalFindingCount} finding in a file is matched.",
                        _ => $"{GrandTotalFindingCount} finding in {GrandTotalFileCount} files are matched.",
                    },
                };
                Console.WriteLine(tmp);
                return Ignore.Void;
            });
    #endregion

    public record CountFound(string Filename, int Count, IConsolePause Pause);

    static public readonly IInvoke<CountFound, bool> PrintIfAnyFound =
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
        IEnumerable<int>, int>
        MaxFound = new ParseInvoker<
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

                if (args[0].Type == ArgType.Never)
                {
                    Log.Debug($"{nameof(MaxFound)} is NEVER to -1.");
                    opt.SetImplementation((seq) =>
                    {
                        var itr = seq.GetEnumerator();
                        while (itr.MoveNext())
                        {
                            if (itr.Current > 0) return -1;
                        }
                        return 0;
                    });
                    return;
                }

                if (opt.Help.Equals(args[0].Arg,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ArgumentException(opt.ExtraHelp);
                }

                if (false == int.TryParse(args[0].Arg, out int takeCount))
                    throw new ConfigException(
                        $"Value to {opt.Name} SHOULD be a number but '{args[0].Arg}' is found!");

                if (1 > takeCount)
                    throw new ConfigException(
                        $"Value to {opt.Name} SHOULD be greater than zero but {takeCount} is found!");

                opt.SetImplementation((seq) =>
                {
                    int sum = 0;
                    var itr = seq.GetEnumerator();
                    while (itr.MoveNext())
                    {
                        sum += itr.Current;
                        if (sum >= takeCount) break;
                    }
                    return sum;
                });
            });

    static public readonly IInvoke<Ignore, Ignore> FilenameOnly =
        new SwitchInvoker<Ignore, Ignore>(TextFileMatch,
            init: Ignore.Maker, alter: Ignore.Maker,
            alterFor: true, alterPost: (flag) =>
            {
                if (true == flag)
                {
                    ((IParse)MaxFound).Parse([new(true, ArgType.Never, "1")]);
                    ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                    ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                    ((IParse?)PrintLineMaker)?.Parse([FlagedArg.Never]);

                    ((SwitchInvoker<CountFound, bool>)PrintIfAnyFound)
                    .SetImplementation((it) =>
                    {
                        if (it.Count != 0)
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
