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
}
