using System.Runtime.InteropServices;
using RegX = System.Text.RegularExpressions;
using static grep.MyOptions;
using static grep.Options;

namespace grep;

internal static partial class Show
{
    public interface IPrintMatchedLine
    {
        int Print(MatchResult arg);
        void SetDefaultColor();
    }

    class FakePrinter : IPrintMatchedLine
    {
        public int Print(MatchResult _) => 0;
        public void SetDefaultColor() { }
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
        public void SetDefaultColor() { }
        static public PrintWithoutColor Default = new();
        PrintWithoutColor() { }
    }

    static bool TryParseToConsoleColor(string arg, out ConsoleColor result)
    {
        if (string.IsNullOrEmpty(arg))
            throw new ArgumentException(
                $"Null param to func {nameof(TryParseToConsoleColor)}");
        result = default;
        if (arg[0].IsNumber()) return false;
        if (Enum.TryParse(typeof(ConsoleColor), arg,
            ignoreCase: true, out var tmp))
        {
            result = (ConsoleColor)tmp;
            return true;
        }
        return false;
    }

    class PrintWithColor(
        ConsoleColor ForegroundHighlightColor,
        ConsoleColor BackgroundHighlightColor,
        int CounterPerGroup,
        ConsoleColor ForegroundColorGroup,
        ConsoleColor BackgroundColorGroup
        ) : IPrintMatchedLine
    {
        static ConsoleColor DefaultForeground = Console.ForegroundColor;
        static ConsoleColor DefaultBackground = Console.BackgroundColor;
        public void SetDefaultColor()
        {
            Console.ForegroundColor = DefaultForeground;
            Console.BackgroundColor = DefaultBackground;
        }

        Action IncreaseLineCount { get; set; } = () =>
        {
            int rtn = LineCount + 1;
            if (rtn >= CounterPerGroup)
            {
                DefaultForeground = ForegroundColorGroup;
                DefaultBackground = BackgroundColorGroup;
                rtn = 0;
            }
            else if (rtn == 1)
            {
                DefaultForeground = OldForegroundColor;
                DefaultBackground = OldBackgroundColor;
            }
            LineCount = rtn;
        };

        static int LineCount = 1;

        public PrintWithColor(
            ConsoleColor ForegroundColor,
            ConsoleColor BackgroundColor) :
            this(ForegroundColor, BackgroundColor,
                1, Console.ForegroundColor, Console.BackgroundColor)
        {
            IncreaseLineCount = () => { };
        }

        public PrintWithColor(
            ConsoleColor ForegroundColor,
            ConsoleColor BackgroundColor,
            int CounterPerGroup,
            ConsoleColor ForegroundColorGroup) :
            this(ForegroundColor, BackgroundColor,
                CounterPerGroup, ForegroundColorGroup, Console.BackgroundColor)
        { }

        static ConsoleColor OldForegroundColor { get; set; } = Console.ForegroundColor;
        static ConsoleColor OldBackgroundColor { get; set; } = Console.BackgroundColor;

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

            if (ndxLast < arg.Line.Length)
            {
                Switch(false);
                Console.Write(arg.Line[ndxLast..]);
            }
            Console.ResetColor();
            Console.WriteLine();
            IncreaseLineCount();
            return arg.Line.Length;
        }

        public void Switch(bool flag)
        {
            if (flag)
            {
                Console.ForegroundColor = ForegroundHighlightColor;
                Console.BackgroundColor = BackgroundHighlightColor;
            }
            else
            {
                Console.ForegroundColor = DefaultForeground;
                Console.BackgroundColor = DefaultBackground;
            }
        }
    }

    static void ShowColorHelp()
    {
        var pause = PauseMaker.Invoke(Ignore.Void);
        Console.WriteLine($"Syntax: {nameof(grep)} {TextColor} COLOR ..");
        pause.Printed(10);

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
            pause.Printed(10);
        }
        Console.WriteLine();
        pause.Printed(10);

        // ........-123456789012345678901234567890
        var hint = "Disable the feature by";
        Console.WriteLine($"{hint,-30}  {nameof(grep)} {TextColor} off");
        pause.Printed(10);
        hint = "Set background color by";
        Console.WriteLine($"{hint,-30}  {nameof(grep)} {TextColor} COLOR,COLOR");
        pause.Printed(10);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            hint = "Invert colors by";
            Console.WriteLine($"{hint,-30}  {nameof(grep)} {TextColor} --");
            pause.Printed(10);
        }

        hint = "Set group color by";
        Console.WriteLine(
            $"{hint,-30}  {nameof(grep)} {TextColor} COLOR,COLOR,NUMBER,COLOR");
        pause.Printed(10);
        hint = "";
        Console.WriteLine(
            $"{hint,-30}  {nameof(grep)} {TextColor} COLOR,COLOR,NUMBER,COLOR,COLOR");
        pause.Printed(10);
        Console.WriteLine($"""

                        Option can be stored by envir var '{nameof(grep)}', for example
                        """);
        pause.Printed(10);
        pause.Printed(10);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine($"""
                        set {nameof(grep)}={TextColor} white,darkred,5,black,yellow
                        """);
            pause.Printed(10);
        }
        else
        {
            Console.WriteLine($"""
                        export {nameof(grep)}="{TextColor} white,darkred,5,black,yellow"
                        """);
            pause.Printed(10);
        }
    }

    [RegX.GeneratedRegex(
        @"^(?<fore>\w{3,12})(\,(?<back>\w{3,12}))?(\,(?<num>\w{1,5}))?(\,(?<foreGroup>\w{3,12}))?(\,(?<backGroup>\w{3,12}))?$")]
    private static partial RegX.Regex RegexColors();

    static public readonly IInvoke<Ignore, IPrintMatchedLine> PrintLineMaker = new
        ParseInvoker<Ignore, IPrintMatchedLine>(TextColor, help: "COLOR | -",
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
            var typeThe = argsThe.First().Type;
            if (typeThe == ArgType.Never)
            {
                opt.SetImplementation((_) => FakePrinter.Empty);
                return;
            }

            var args = argsThe.Distinct().Take(2).ToArray();
            if (args.Length > 1)
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Too many value ('{args[0]}','{args[1]}') to {opt.Name}"),
                    option: opt);
                return;
            }

            if (args.Length == 0) return;

            var argThe = args[0].Arg;
            if (argThe.Equals("color", StringComparison.InvariantCultureIgnoreCase)
            || ("-" == argThe))
            {
                if (args[0].Type == ArgType.CommandLine)
                {
                    ShowColorHelp();
                    Environment.Exit(0);
                }
                else
                {
                    ConfigException.Add(args[0].Type, new ArgumentException(
                        "Help of color is ignored."));
                    return;
                }
            }

            if (TextOff.Equals(argThe,
                StringComparison.InvariantCultureIgnoreCase))
            {
                opt.SetImplementation((_) => PrintWithoutColor.Default);
                return;
            }

            if (Console.IsOutputRedirected)
            {
                opt.SetImplementation((_) => PrintWithoutColor.Default);
                return;
            }

            if (argThe.Equals("--"))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    opt.SetImplementation((_) => new PrintWithColor(
                        ForegroundColor: Console.BackgroundColor,
                        BackgroundColor: Console.ForegroundColor));
                }
                else
                {
                    Log.Debug("{0}> OS='{1}'", typeThe, RuntimeInformation.OSDescription);
                    ConfigException.Add(typeThe, new ArgumentException(
                        $"Inverse to {opt.Name} is NOT supported in the running OS."));
                }
                return;
            }

            var matchThe = RegexColors().Match(argThe);
            if (true != matchThe.Success)
            {
                ConfigException.WrongValue(args[0], opt);
                return;
            }

            var tmp2 = matchThe.Groups["fore"].Value;
            if (string.IsNullOrEmpty(tmp2))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Foreground to {opt.Name} is required!"),
                    option: opt);
                return;
            }
            ConsoleColor foreColor = ConsoleColor.Red;
            if (false == TryParseToConsoleColor(tmp2, out foreColor))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Foreground '{tmp2}' to {opt.Name} is NOT color!"),
                    option: opt);
                return;
            }

            tmp2 = matchThe.Groups["back"].Value;
            if (string.IsNullOrEmpty(tmp2))
            {
                opt.SetImplementation((_) => new PrintWithColor(
                    ForegroundColor: foreColor,
                    BackgroundColor: Console.ForegroundColor));
                return;
            }

            ConsoleColor backColor = Console.BackgroundColor;
            if (false == TryParseToConsoleColor(tmp2, out backColor))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Background '{tmp2}' to {opt.Name} is NOT color!"),
                    option: opt);
                return;
            }

            tmp2 = matchThe.Groups["num"].Value;
            if (string.IsNullOrEmpty(tmp2))
            {
                opt.SetImplementation((_) => new PrintWithColor(
                    ForegroundColor: foreColor,
                    BackgroundColor: backColor));
                return;
            }

            if (false == int.TryParse(tmp2, out int countPerGroup))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Count-Per-Group '{tmp2}' to {opt.Name} is NOT a number!"),
                    option: opt);
                return;
            }

            if (countPerGroup < 2)
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Count-Per-Group to {opt.Name} MUST be greater than 1 but {countPerGroup} is found!"),
                    option: opt);
                return;
            }
            countPerGroup += 1;

            tmp2 = matchThe.Groups["foreGroup"].Value;
            if (string.IsNullOrEmpty(tmp2))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Foreground-Group to {opt.Name} is required!"),
                    option: opt);
                return;
            }
            ConsoleColor foreGroup = Console.ForegroundColor;
            if (false == TryParseToConsoleColor(tmp2, out foreGroup))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Foreground-Group '{tmp2}' to {opt.Name} is NOT color!"),
                    option: opt);
                return;
            }

            tmp2 = matchThe.Groups["backGroup"].Value;
            if (string.IsNullOrEmpty(tmp2))
            {
                opt.SetImplementation((_) => new PrintWithColor(
                    foreColor, backColor, countPerGroup, foreGroup));
                return;
            }
            ConsoleColor backGroup = Console.BackgroundColor;
            if (false == TryParseToConsoleColor(tmp2, out backGroup))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Background-Group '{tmp2}' to {opt.Name} is NOT color!"),
                    option: opt);
                return;
            }
            opt.SetImplementation((_) => new PrintWithColor(
                foreColor, backColor,
                countPerGroup, foreGroup, backGroup));
        });
}
