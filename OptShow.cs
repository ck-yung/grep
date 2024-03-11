using static grep.MyOptions;

namespace grep;

internal static class Show
{
    static public readonly IInovke<string, Ignore> Filename =
        new SwitchInovker<string, Ignore>(Options.OptShowFilename,
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
    static public readonly IInovke<int, bool> LineNumber =
        new SwitchInovker<int, bool>(Options.OptLineNumber,
            init: Always<int>.True,
            alterFor: true, alter: (lineNumber) =>
            {
                Console.Write($"{1 + lineNumber}:");
                return true;
            });

    static public Action<string> PrintMatchedLine { get; private set; } = (line) =>
    {
        Console.WriteLine(line);
    };

    static public readonly IInovke<(string, int), Ignore> FoundCount =
        new SwitchInovker<(string, int), Ignore>(Options.OptCountOnly,
            init: Ignore<(string, int)>.Maker,
            alterFor: true, alterWhen: (_) =>
            {
                ((IParse)Filename).Parse(Options.TextOff
                    .ToFlagedArgs(ArgType.CommandLine));
                ((IParse)LineNumber).Parse(Options.TextOff
                    .ToFlagedArgs(ArgType.CommandLine));
                PrintMatchedLine = (_) => { };
            },
            alter: (it) =>
            {
                Console.WriteLine($"{it.Item1}:{it.Item2}");
                return Ignore.Void;
            });
}
