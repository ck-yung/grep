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
    static Action<int> PrintAction = (_) => { };

    public static void EnablePrint(bool flag)
    {
        if (flag)
        {
            PrintAction =
                (number) => Console.Write($"{number}:");
        }
        else
        {
            PrintAction = (_) => { };
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
    public static bool Print(int lineNumber)
    {
        PrintAction(lineNumber);
        Index += 1;
        return CheckByMax(Index);
    }
}

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

    public static bool Init(string color)
    {
        if (0 == string.Compare("off", color, ignoreCase: true))
        {
            Disable();
            return true;
        }

        if (0 == string.Compare("color", color, ignoreCase: true))
        {
            Help();
            throw new MissingValueException("");
        }

        if (0 == string.Compare("inverse", color, ignoreCase: true))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                HighlightForeground = Console.BackgroundColor;
                HighlightBackground = Console.ForegroundColor;
            }
            else
            {
                HighlightForeground = ConsoleColor.Red;
                HighlightBackground = Console.BackgroundColor;
            }
            return true;
        }

        OldForeground = Console.ForegroundColor;
        OldBackground = Console.BackgroundColor;
        if (color.StartsWith("!"))
        {
            if (TryParseToForeColor(color[1..], out var tmp))
            {
                HighlightForeground = Console.ForegroundColor;
                HighlightBackground = tmp;
            }
            else
            {
                throw new ArgumentException($"""
                '{color[1..]}' is NOT color name. Please run below to show color:
                  grep --color
                """);
            }
        }
        else
        {
            if (TryParseToForeColor(color, out var tmp))
            {
                HighlightForeground = tmp;
            }
            else
            {
                throw new ArgumentException($"""
                '{color}' is NOT color name. Please run below to show color:
                  grep --color
                """);
            }
        }
        return true;
    }

    public static void Disable()
    {
        Swith = () => { };
        Reset = () => { };
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

        Action<bool, ConsoleColor> switchBackgroundColor = (isBlack, arg) =>
        {
            Console.ForegroundColor = arg;
            switch (isBlack, arg)
            {
                case (_, ConsoleColor.Black):
                    Console.BackgroundColor = ConsoleColor.Gray;
                    break;
                case (_, ConsoleColor.Gray):
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case (true, _):
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                default:
                    Console.BackgroundColor = ConsoleColor.Gray;
                    break;
            }
            Console.Write(isBlack ? " Good " : " Demo ");
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
            Console.WriteLine();
        }

        return string.Empty;
    }
}
