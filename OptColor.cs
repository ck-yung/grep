using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
            throw new ArgumentNullException(
                $"Parm to func {nameof(TryParseToConsoleColor)}");
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
        int CounterPerBatch,
        ConsoleColor ForegroundColorBatch,
        ConsoleColor BackgroundColorBatch

        ) : IPrintMatchedLine
    {
        static ConsoleColor DefaultForeground = Console.ForegroundColor;
        static ConsoleColor DefaultBackground = Console.BackgroundColor;
        public void SetDefaultColor()
        {
            Console.ForegroundColor = DefaultForeground;
            Console.BackgroundColor = DefaultBackground;
        }

        Action IncreaseLineCount = () =>
        {
            int rtn = LineCount + 1;
            if (rtn >= CounterPerBatch)
            {
                DefaultForeground = ForegroundColorBatch;
                DefaultBackground = BackgroundColorBatch;
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
            int CounterPerBatch,
            ConsoleColor ForegroundColorBatch) :
            this(ForegroundColor, BackgroundColor,
                CounterPerBatch, ForegroundColorBatch, Console.BackgroundColor)
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
        Console.WriteLine($"Syntax: {nameof(grep)} {TextColor} COLOR ..");

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
        var hint = "Disable the feature by";
        Console.WriteLine($"{hint,-30}  {nameof(grep)} {TextColor} off ..");

        hint = "Assign background color by";
        Console.WriteLine($"{hint,-30}  {nameof(grep)} {TextColor} COLOR,BACKGROUND-COLOR ..");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            hint = "Invert colors by";
            Console.WriteLine($"{hint,-30}  {nameof(grep)} {TextColor} -- ..");
        }

        hint = "Change color per batch by";
        Console.WriteLine(
            $"{hint,-30}  {nameof(grep)} {TextColor} COLOR,COLOR,NUMBER,COLOR ..");
        hint = "";
        Console.WriteLine(
            $"{hint,-30}  {nameof(grep)} {TextColor} COLOR,COLOR,NUMBER,COLOR,COLOR ..");

        Console.WriteLine($"""

                        Option can be stored by envir var '{nameof(grep)}', for example
                        """);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine($"""
                        set {nameof(grep)}={TextColor} red,darkyellow,9,black,gray
                        """);
        }
        else
        {
            Console.WriteLine($"""
                        export {nameof(grep)}="{TextColor} red,darkyellow,9,black,gray"
                        """);
        }
    }

    [GeneratedRegex(
        @"^(?<fore>\w{3,12})(\,(?<back>\w{3,12}))?(\,(?<num>\w{1,5}))?(\,(?<foreBatch>\w{3,12}))?(\,(?<backBatch>\w{3,12}))?$")]
    private static partial Regex RegexColors();

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

            var args = argsThe.Select((it) => it.Arg).Distinct().Take(2).ToArray();
            if (args.Length > 1)
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Too many value ('{args[0]}','{args[1]}') to {opt.Name}"),
                    option: opt);
                return;
            }

            var argThe = args[0];
            if (opt.Help.Equals(argThe, StringComparison.InvariantCultureIgnoreCase)
            || ("-" == argThe))
            {
                ShowColorHelp();
                Environment.Exit(0);
            }

            if (TextOff.Equals(argThe,
                StringComparison.InvariantCultureIgnoreCase))
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
                    Log.Debug($"{typeThe}> OS='{RuntimeInformation.OSDescription}'");
                    ConfigException.Add(typeThe, new ArgumentException(
                        $"Inverse to {opt.Name} is NOT supported in the running OS."));
                }
                return;
            }

            var matchThe = RegexColors().Match(argThe);
            if (true != matchThe.Success)
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Value '{argThe}' to {opt.Name} is NOT valid!"));
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

            int countPerBatch;
            if (false == int.TryParse(tmp2, out countPerBatch))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Count-Batch '{tmp2}' to {opt.Name} is NOT a number!"),
                    option: opt);
                return;
            }

            if (countPerBatch < 2)
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Count-Batch to {opt.Name} MUST be greater than 1 but {countPerBatch} is found!"),
                    option: opt);
                return;
            }
            countPerBatch += 1;

            tmp2 = matchThe.Groups["foreBatch"].Value;
            if (string.IsNullOrEmpty(tmp2))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Foreground-Batch to {opt.Name} is required!"),
                    option: opt);
                return;
            }
            ConsoleColor foreBatch = Console.ForegroundColor;
            if (false == TryParseToConsoleColor(tmp2, out foreBatch))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Foreground-Batch '{tmp2}' to {opt.Name} is NOT color!"),
                    option: opt);
                return;
            }

            tmp2 = matchThe.Groups["backBatch"].Value;
            if (string.IsNullOrEmpty(tmp2))
            {
                opt.SetImplementation((_) => new PrintWithColor(
                    foreColor, backColor, countPerBatch, foreBatch));
                return;
            }
            ConsoleColor backBatch = Console.BackgroundColor;
            if (false == TryParseToConsoleColor(tmp2, out backBatch))
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Background-Batch '{tmp2}' to {opt.Name} is NOT color!"),
                    option: opt);
                return;
            }
            opt.SetImplementation((_) => new PrintWithColor(
                foreColor, backColor,
                countPerBatch, foreBatch, backBatch));
        });
}
