using System.Runtime.InteropServices;

namespace grep;

internal class Always<T>
{
    static public readonly Func<T, bool> True = (_) => true;
}

internal class Never<T>
{
    static public readonly Func<T, bool> Holds = (_) => false;
}

internal static class Counter
{
    /// <param>Line Number</param>
    /// <returns>Counter of char is printed.</returns>
    static Func<int, int> PrintAction = (_) => 0;

    public static void EnablePrint(bool flag)
    {
        if (flag)
        {
            PrintAction = (number) =>
            {
                var tmp = number.ToString() + ":";
                Console.Write(tmp);
                return tmp.Length;
            };
        }
        else
        {
            PrintAction = (_) => 0;
        }
    }

    public static void Reset()
    {
        Index = 0;
    }

    static int Index = 0;
    static Func<int, bool> CheckByMax { get; set; } = Always<int>.True;

    public static bool SetLimit(int max)
    {
        if (max < 1) return false;
        CheckByMax = (number) => number < max;
        return true;
    }

    /// <returns>False if no more printing is allowed</returns>
    public static PrintLineNumberResult Print(int lineNumber)
    {
        var cntCharPrinted = PrintAction(lineNumber);
        Index += 1;
        return new(CheckByMax(Index), cntCharPrinted);
    }
}

record PrintLineNumberResult(bool IsContinuous, int CounterOfCharPrinted);

record WildFileResult(string[] Args,
    Func<string, MatchCollecton> Match);

static class ColorCfg
{
    static bool State = false;
    static ConsoleColor OldForeground { get; set; }
    static ConsoleColor OldBackground { get; set; }
    static ConsoleColor HighlightForeground { get; set; }
    static ConsoleColor HighlightBackground { get; set; }

    static bool TryParseToForeColor(string arg, out ConsoleColor output)
    {
        bool rtn = false;
        if (Enum.TryParse(typeof(ConsoleColor), arg,
            ignoreCase: true, out var result))
        {
            output = (ConsoleColor)result;
            rtn = true;
        }
        else
        {
            output = ConsoleColor.White;
        }
        return rtn;
    }

    public static Ignore Init(IEnumerable<string> color)
    {
        var colors = color
            .Select((it) => it.Trim())
            .Where((it) => it.Length > 0)
            .Distinct().Order().ToArray();

        OldForeground = Console.ForegroundColor;
        OldBackground = Console.BackgroundColor;
        HighlightForeground = ConsoleColor.Red;
        HighlightBackground = Console.BackgroundColor;

        if (colors.Length == 0) return Ignore.Void;

        const string offText = "off";
        const string colorText = "color";
        const string inverseText = "inverse";

        if (colors.Contains(offText))
        {
            return Disable();
        }

        if (colors.Contains(colorText) || colors.Contains("~color"))
        {
            Help();
            throw new MissingValueException("");
        }

        if (colors.Contains(inverseText))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                HighlightForeground = Console.BackgroundColor;
                HighlightBackground = Console.ForegroundColor;
            }
            else
            {
                HighlightForeground = ConsoleColor.Red;
            }
            return Ignore.Void;
        }

        static (bool, ConsoleColor) ParseColor(string arg)
        {
            static string errorMessage(string name) => $"""
                '{name}' is NOT color name. Please run below to show color:
                  grep --color
                """;

            if (arg.StartsWith('~'))
            {
                arg = arg[1..];
                if (TryParseToForeColor(arg, out var tmp))
                {
                    return (false, tmp);
                }
                throw new ArgumentException(errorMessage(arg));
            }

            if (TryParseToForeColor(arg, out var tmp2))
            {
                return (true, tmp2);
            }
            throw new ArgumentException(errorMessage(arg));
        }

        var errorMsg = """
            Syntax: grep --color  FOREGROUND-COLOR
            Syntax: grep --color ~BACKGROUND-COLOR
            """;

        switch (colors)
        {
            case [string color1]:
                (var isForeground1, var colorThe1) = ParseColor(color1);
                if (isForeground1)
                {
                    HighlightForeground = colorThe1;
                }
                else
                {
                    HighlightForeground = OldForeground;
                    HighlightBackground = colorThe1;
                }
                break;
            case [string color2, string color3]:
                (var isForeground2, var colorThe2) = ParseColor(color2);
                (var isForeground3, var colorThe3) = ParseColor(color3);
                if (isForeground2 == isForeground3)
                    throw new ArgumentException(errorMsg);
                if (isForeground2 == true) throw new ArgumentException("Unknown error2");
                HighlightBackground = colorThe2;
                HighlightForeground = colorThe3;
                break;
            default:
                throw new ArgumentException(errorMsg);
        }
        return Ignore.Void;
    }

    public static Ignore Disable()
    {
        Swith = () => { };
        Reset = () => { };
        return Ignore.Void;
    }

    public static Action Swith { get; private set; } = () =>
    {
        Console.ForegroundColor = State ? HighlightForeground : OldForeground;
        Console.BackgroundColor = State ? HighlightBackground : OldBackground;
        State = !State;
    };

    public static Action Reset { get; private set; } = () =>
    {
        State = false;
        Console.ResetColor();
    };

    public static string Help()
    {
        if (Console.IsOutputRedirected)
            return "No help of COLOR could be provided while console output is redir.";

        void switchBackgroundColor(bool isBlack, ConsoleColor arg)
        {
            Console.ForegroundColor = arg;
            Console.BackgroundColor = (isBlack, arg) switch
            {
                (_, ConsoleColor.Gray) => ConsoleColor.DarkGray,
                (_, ConsoleColor.DarkGray) => ConsoleColor.Gray,
                (true, _) => ConsoleColor.DarkGray,
                _ => ConsoleColor.Gray,
            };
            Console.Write(isBlack ? " Good " : " Demo ");
        };

        void switchForegroundColor(ConsoleColor arg)
        {
            Console.BackgroundColor = arg;
            Console.ForegroundColor = (arg) switch
            {
                (ConsoleColor.DarkGray) => ConsoleColor.Blue,
                _ => ConsoleColor.DarkGray,
            };
            Console.Write(" Background ");
        };

        Console.WriteLine($"Value to '--color' :");
        //                    123456789.12.
        Console.WriteLine($"\tOff          Disable");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        { //                      123456789.12.
            Console.WriteLine($"\tInverse      Switch Background and Foreground");
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
        }

        return string.Empty;
    }
}
