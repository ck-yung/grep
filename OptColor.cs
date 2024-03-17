using System.Drawing;
using System.Runtime.InteropServices;
using static grep.MyOptions;
using static grep.Options;

namespace grep;

internal static partial class Show
{
    public interface IPrintMatchedLine
    {
        int Print(MatchResult arg);
    }

    class FakePrinter : IPrintMatchedLine
    {
        public int Print(MatchResult _) => 0;
        static public FakePrinter Empty = new();
        FakePrinter() { }
    }

    class PrintWithoutColor : IPrintMatchedLine
    {
        public int Print(MatchResult it)
        {
            Console.WriteLine(it.Line);
            return it.Line.Length;
        }
        static public PrintWithoutColor Default = new();
        PrintWithoutColor() { }
    }

    static bool TryParseToConsoleColor(string arg, out ConsoleColor result)
    {
        if (Enum.TryParse(typeof(ConsoleColor), arg,
            ignoreCase: true, out var tmp))
        {
            result = (ConsoleColor)tmp;
            return true;
        }
        throw new ArgumentException($"'{arg}' is NOT color.");
    }

    class PrintWithColor(ConsoleColor ForegroundColor,
        ConsoleColor BackgroundColor) : IPrintMatchedLine
    {
        ConsoleColor OldForegroundColor { get; set; } = Console.ForegroundColor;
        ConsoleColor OldBackgroundColor { get; set; } = Console.BackgroundColor;

        public int Print(MatchResult arg)
        {
            var ndxLast = 0;
            foreach ((var ndx, var len) in arg.Matches)
            {
                Switch(false);
                switch (ndxLast, ndx)
                {
                    case (0, 0): break;
                    default:
                        Console.Write(arg.Line[ndxLast..ndx]);
                        break;
                }
                Switch(true);
                Console.Write(arg.Line[ndx..(ndx + len)]);
                ndxLast = ndx + len;
            }
            Console.ResetColor();
            if (ndxLast < arg.Line.Length)
            {
                Console.Write(arg.Line[ndxLast..]);
            }
            Console.WriteLine();
            return arg.Line.Length;
        }

        public void Switch(bool flag)
        {
            if (flag)
            {
                Console.ForegroundColor = ForegroundColor;
                Console.BackgroundColor = BackgroundColor;
            }
            else
            {
                Console.ForegroundColor = OldForegroundColor;
                Console.BackgroundColor = OldBackgroundColor;
            }
        }
    }

    static public readonly IInvoke<Ignore, IPrintMatchedLine> PrintLineMaker = new
        ParseInvoker<Ignore, IPrintMatchedLine>(TextColor, help: "COLOR",
        extraHelp: $"For help, please run: {nameof(grep)} {TextColor} -",
        init: (_) =>
        {
            if (Console.IsOutputRedirected)
                return PrintWithoutColor.Default;
            return new PrintWithColor(ConsoleColor.Red,
                Console.BackgroundColor);
        },
        resolve: (opt, argsThe) =>
        {
            if (Console.IsOutputRedirected)
            {
                opt.SetImplementation((_) => PrintWithoutColor.Default);
                return;
            }

            var typeThe = argsThe.First().Type;
            if (typeThe == ArgType.Never)
            {
                opt.SetImplementation((_) => FakePrinter.Empty);
                return;
            }

            var args = argsThe.Select((it) => it.Arg).Distinct().Take(3).ToArray();
            if (args.Length > 2)
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Too many value ('{args[0]}','{args[1]}','{args[2]}') to {opt.Name}"),
                    option: opt);
                return;
            }

            // color demo
            if (("-" == args[0]) ||
            opt.Help.Equals(args[0], StringComparison.InvariantCultureIgnoreCase) ||
            ((args.Length > 1) && ("-" == args[1])))
            {
                Console.WriteLine($"Syntax: {nameof(grep)} {TextColor}  COLOR ..");

                static void switchBackgroundColor(bool isBlack, ConsoleColor arg)
                {
                    Console.ForegroundColor = arg;
                    Console.BackgroundColor = (isBlack, arg) switch
                    {
                        (_, ConsoleColor.Black) => ConsoleColor.Gray,
                        (_, ConsoleColor.Gray) => ConsoleColor.Black,
                        (true, _) => ConsoleColor.Black,
                        _ => ConsoleColor.Gray,
                    };
                    Console.Write(isBlack ? " Good " : " Demo ");
                }

                static void switchForegroundColor(ConsoleColor arg)
                {
                    Console.BackgroundColor = arg;
                    Console.ForegroundColor = arg switch
                    {
                        (ConsoleColor.Black) => ConsoleColor.Gray,
                        (ConsoleColor.DarkGray) => ConsoleColor.White,
                        _ => ConsoleColor.Black,
                    };
                    Console.Write(" Blackground ");
                }
                Console.WriteLine($"where COLOR is one of the following:");
                foreach (ConsoleColor cr2 in Enum.GetValues(typeof(ConsoleColor)))
                {
                    Console.Write($"\t{cr2,-12}");
                    switchBackgroundColor(true, cr2);
                    switchBackgroundColor(false, cr2);
                    Console.ResetColor();
                    Console.Write("  ");
                    switchForegroundColor(cr2);
                    Console.ResetColor();
                    Console.WriteLine();
                }
                Console.WriteLine();

                // ........-123456789012345678901234567890
                var hint = "Assign background color by";
                Console.WriteLine($"{hint,-30}  {nameof(grep)} {TextColor} ~COLOR ..");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    hint = "Exchange colors by";
                    Console.WriteLine($"{hint,-30}  {nameof(grep)} {TextColor} ~ ..");
                }

                hint = "Disable the feature by";
                Console.WriteLine($"{hint,-30}  {nameof(grep)} {TextColor} off ..");

                Console.WriteLine($"""

                            Option can be assigned by envir var '{nameof(grep)}', for example
                            """);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.WriteLine($"""
                            set {nameof(grep)}={TextColor} black; {TextColor} ~yellow
                            """);
                }
                else
                {
                    Console.WriteLine($"""
                            export {nameof(grep)}="{TextColor} black; {TextColor} ~yellow"
                            """);
                }
                Environment.Exit(0);
            }

            if (args.Length < 2)
            {
                var argFirst = args[0];
                if (TextOff.Equals(argFirst,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    opt.SetImplementation((_) => PrintWithoutColor.Default);
                    return;
                }

                if (argFirst.Equals("~"))
                {
                    if (false == RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        ConfigException.Add(typeThe, new ArgumentException(
                            $"Inverse to {opt.Name} is NOT supporte in the running OS."));
                    }
                    opt.SetImplementation((_) => new PrintWithColor(
                        ForegroundColor: Console.BackgroundColor,
                        BackgroundColor: Console.ForegroundColor));
                    return;
                }

                (bool isBackground, string colorText) = (argFirst.StartsWith('~'), argFirst);
                if (isBackground)
                {
                    colorText = colorText[1..];
                }
                if (TryParseToConsoleColor(colorText, out var colorThe))
                {
                    if (isBackground)
                    {
                        opt.SetImplementation((_) => new PrintWithColor(
                            ConsoleColor.Red, colorThe));
                    }
                    else
                    {
                        opt.SetImplementation((_) => new PrintWithColor(colorThe,
                            Console.BackgroundColor));
                    }
                }
                else
                {
                    ConfigException.Add(typeThe, new ArgumentException(
                        $"'{argFirst}' to {opt.Name} is NOT valid!"),
                        option: opt);
                }
                return;
            }

            bool isHighForeOk;
            bool isHighBackOk;
            ConsoleColor highFore;
            ConsoleColor highBack;
            switch (args[0].StartsWith('~'), args[1].StartsWith('~'))
            {
                case (true, false):
                    isHighBackOk = TryParseToConsoleColor(args[0][1..], out highBack);
                    isHighForeOk = TryParseToConsoleColor(args[1], out highFore);
                    break;
                case (false, true):
                    isHighBackOk = TryParseToConsoleColor(args[1][1..], out highBack);
                    isHighForeOk = TryParseToConsoleColor(args[0], out highFore);
                    break;
                default:
                    ConfigException.Add(typeThe, new ArgumentException(
                        $"Values '{args[0]}' and '{args[1]}' to {opt.Name} are NOT valid!"),
                        option: opt);
                    return;
            }

            if (isHighBackOk && isHighForeOk)
            {
                opt.SetImplementation((_) => new PrintWithColor(highFore, highBack));
            }
            else
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Values '{args[0]}' and '{args[1]}' to {opt.Name} are NOT valid!"),
                    option: opt);
            }
        });
}
