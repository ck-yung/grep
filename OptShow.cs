using static grep.MyOptions;
using static grep.Options;

namespace grep;

internal static class Show
{
    static public readonly IInvoke<string, Ignore> Filename =
        new SwitchInvoker<string, Ignore>(OptShowFilename,
            init: (filename) =>
            {
                Console.Write($"{filename}:");
                return Ignore.Void;
            },
            alterFor: false, alter: Ignore<string>.Maker);

    /// <summary>
    /// Invoke(string) return
    /// true, if continue
    /// false, if break for scanning the file
    /// </summary>
    static public readonly IInvoke<int, bool> LineNumber =
        new SwitchInvoker<int, bool>(OptLineNumber,
            init: Always<int>.True,
            alterFor: true, alter: (lineNumber) =>
            {
                Console.Write($"{1 + lineNumber}:");
                return true;
            });

    static public Action<string> PrintMatchedLine { get; private set; } =
        (line) => Console.WriteLine(line);

    static public readonly IInvoke<(string, int), Ignore> FoundCount =
        new SwitchInvoker<(string, int), Ignore>(OptCountOnly,
            init: Ignore<(string, int)>.Maker,
            alterFor: true, alterPost: (flag) =>
            {
                if (true == flag)
                {
                    ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                    ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                    PrintMatchedLine = (_) => { };
                }
            },
            alter: (it) =>
            {
                Console.WriteLine($"{it.Item1}:{it.Item2}");
                return Ignore.Void;
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
            name: Options.OptMaxCount, init: Helper.Itself,
            resolve: (opt, argsThe) =>
            {
                var args = argsThe.Distinct().Take(2).ToArray();
                if (args.Length > 1)
                {
                    throw new ArgumentException(
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
                        throw new ArgumentException(
                            $"Value to {opt.Name} SHOULD be greater than zero but {takeCount} is found!");
                    }
                }
                else
                {
                    throw new ArgumentException(
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
                PrintMatchedLine = (_) => { };
                ((SwitchInvoker<(string, int), Ignore>)FoundCount)
                .SetImplementation((it) =>
                {
                    Console.WriteLine($"{it.Item1}");
                    return Ignore.Void;
                });
            }
        });
}
