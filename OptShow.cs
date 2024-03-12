using static grep.MyOptions;
using static grep.Options;

namespace grep;

internal static class Show
{
    static public readonly IInvoke<string, int> Filename =
        new SwitchInvoker<string, int>(OptShowFilename,
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
        new SwitchInvoker<int, int>(OptLineNumber,
            init: (_) => 0,
            alterFor: true, alter: (lineNumber) =>
            {
                var msg = $"{1 + lineNumber}:";
                Console.Write(msg);
                return msg.Length;
            });

    static public Func<string, int> PrintMatchedLine { get; private set; } =
        (line) =>
        {
            Console.WriteLine(line);
            return line.Length;
        };

    public record CountFound(IConsolePause Pause, string Filename, int Count);

    static public readonly IInvoke<CountFound, bool> FoundCount =
        new SwitchInvoker<CountFound, bool>(OptCountOnly,
            init: (it) => it.Count > 0,
            alterFor: true, alterPost: (flag) =>
            {
                if (true == flag)
                {
                    ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                    ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                    PrintMatchedLine = (_) => 0;
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
        new SwitchInvoker<string, Ignore>(OptQuiet,
            init: (msg) =>
            {
                Console.WriteLine(msg);
                return Ignore.Void;
            },
            alterFor: true, alter: Ignore<string>.Maker);

    static public readonly IInvoke<
        IEnumerable<MatchResult>, IEnumerable<MatchResult>>
        MyTake = new ParseInvoker<
            IEnumerable<MatchResult>, IEnumerable<MatchResult>>(
            name: OptMaxCount, init: Helper.Itself,
            resolve: (opt, argsThe) =>
            {
                var args = argsThe.Distinct().Take(2).ToArray();
                if (args.Length > 1)
                {
                    throw new ConfigException(
                        $"Too many values ({args[0]},{args[1]}) to {opt.Name}");
                }
                if (int.TryParse(args[0], out var takeCount))
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
                        $"Value to {opt.Name} SHOULD be a number but '{args[0]}' is found!");
                }
            });

    static public readonly IInvoke<Ignore, Ignore> FilenameOnly =
        new SwitchInvoker<Ignore, Ignore>(OptFileMatch,
            init: Ignore.Maker, alter: Ignore.Maker,
            alterFor: true, alterPost: (flag) =>
            {
                if (true == flag)
                {
                    ((IParse)MyTake).Parse("1".ToFlagedArgs());
                    ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                    ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                    PrintMatchedLine = (_) => 0;
                    ((SwitchInvoker<(IConsolePause, string, int), bool>)
                    FoundCount).SetImplementation((it) =>
                    {
                        if (it.Item3 > 0)
                        {
                            Console.WriteLine(it.Item2);
                            it.Item1.Printed(it.Item2.Length);
                            return true;
                        }
                        return false;
                    });
                }
            });

    static public readonly IInvoke<Ignore, IConsolePause> PauseMaker =
        new SwitchInvoker<Ignore, IConsolePause>(OptPause,
            init: (_) => new ConsolePause(),
            alterFor: false,
            alter: (_) => new FakePause());
}
