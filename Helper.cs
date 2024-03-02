namespace grep;

static class ForeColor
{
    static bool State = false;
    static ConsoleColor OldForeground { get; set; }
    static ConsoleColor FoundForeground { get; set; }

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

        OldForeground = Console.ForegroundColor;
        if (TryParseToForeColor(color, out var tmp))
        {
            FoundForeground = tmp;
        }
        else
        {
            throw new ArgumentException($"""
                '{color}' is NOT color name. Please run below to show color:
                  grep --color
                """);
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
        Console.ForegroundColor = State ? FoundForeground : OldForeground;
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
