using System.Runtime.InteropServices;
using static grep.MyOptions;
using static grep.Options;

namespace grep;

internal static class Show
{
    static public readonly IInvoke<string, int> Filename =
        new SwitchInvoker<string, int>(TextShowFilename,
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
        new SwitchInvoker<int, int>(TextLineNumber,
            init: (_) => 0,
            alterFor: true, alter: (lineNumber) =>
            {
                var msg = $"{1 + lineNumber}:";
                Console.Write(msg);
                return msg.Length;
            });

    public record CountFound(IConsolePause Pause, string Filename, int Count);

    static public readonly IInvoke<CountFound, bool> FoundCount =
        new SwitchInvoker<CountFound, bool>(TextCountOnly,
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
        new SwitchInvoker<string, Ignore>(TextQuiet,
            init: (msg) =>
            {
                Console.WriteLine(msg);
                return Ignore.Void;
            },
            alterFor: true, alter: Ignore<string>.Maker);

    static public readonly IInvoke<
        IEnumerable<MatchResult>, IEnumerable<MatchResult>>
        MaxFound = new ParseInvoker<
            IEnumerable<MatchResult>, IEnumerable<MatchResult>>(
            name: TextMaxCount, help: "NUMBER", init: Helper.Itself,
            extraHelp: $"For example, {nameof(grep)} {TextMaxCount} 2 ..",
            resolve: (opt, argsThe) =>
            {
                var args = argsThe.Distinct().Take(2).ToArray();
                if (args.Length > 1)
                {
                    throw new ConfigException(
                        $"Too many values ({args[0].Arg},{args[1].Arg}) to {opt.Name}");
                }
                if (int.TryParse(args[0].Arg, out var takeCount))
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
                        $"Value to {opt.Name} SHOULD be a number but '{args[0].Arg}' is found!");
                }
            });

    static public readonly IInvoke<Ignore, Ignore> FilenameOnly =
        new SwitchInvoker<Ignore, Ignore>(TextFileMatch,
            init: Ignore.Maker, alter: Ignore.Maker,
            alterFor: true, alterPost: (flag) =>
            {
                if (true == flag)
                {
                    ((IParse)MaxFound).Parse("1".ToFlagedArgs());
                    ((IParse)Filename).Parse(TextOff.ToFlagedArgs());
                    ((IParse)LineNumber).Parse(TextOff.ToFlagedArgs());
                    PrintMatchedLine = (_) => 0;
                    ((SwitchInvoker<CountFound, bool>)
                    FoundCount).SetImplementation((it) =>
                    {
                        if (it.Count > 0)
                        {
                            Console.WriteLine(it.Filename);
                            it.Pause.Printed(it.Filename.Length);
                            return true;
                        }
                        return false;
                    });
                }
            });

    #region Color
    static int PrintLineWithoutColor(MatchResult arg)
    {
        Console.WriteLine(arg.Line);
        return arg.Line.Length;
    }
    static int PrintLineColor(MatchResult arg)
    {
        var ndxLast = 0;
        foreach ((var ndx, var len) in arg.Matches)
        {
            SwitchColor.Invoke(false);
            switch (ndxLast, ndx)
            {
                case (0, 0): break;
                default:
                    Console.Write(arg.Line[ndxLast..ndx]);
                    break;
            }
            SwitchColor.Invoke(true);
            Console.Write(arg.Line[ndx..(ndx + len)]);
            ndxLast = ndx + len;
        }
        Console.ResetColor();
        if (ndxLast < arg.Line.Length)
        {
            Console.Write(arg.Line[ndxLast..]);
        }
        Console.ResetColor();
        Console.WriteLine();
        return arg.Line.Length;
    }
    static public Func<MatchResult, int> PrintMatchedLine { get; private set; } =
        (arg) => PrintLineColor(arg);

    static ConsoleColor OldForeColor { get; set; } = Console.ForegroundColor;
    static ConsoleColor OldBackColor { get; set; } = Console.BackgroundColor;
    static ConsoleColor HighlightForeColor { get; set; } = ConsoleColor.Red;
    static ConsoleColor HighlightBackColor { get; set; } = Console.BackgroundColor;
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

    static public readonly IInvoke<bool, Ignore> SwitchColor = new ParseInvoker
        <bool, Ignore>(TextColor, help: "COLOR",
        extraHelp: $"'{nameof(grep)} {TextColor} -' for help",
        init: (flag) =>
        {
            if (flag)
            {
                Console.ForegroundColor = HighlightForeColor;
                Console.BackgroundColor = HighlightBackColor;
            }
            else
            {
                Console.ForegroundColor = OldForeColor;
                Console.BackgroundColor = OldBackColor;
            }
            return Ignore.Void;
        },
        resolve: (opt, argsThe) =>
        {
            var typeThe = argsThe.First().Type;
            var args = argsThe.Select((it) => it.Arg).Distinct().Take(3).ToArray();
            if (args.Length > 2)
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Too many value ('{args[0]}','{args[1]}','{args[2]}') to {opt.Name}"));
                return;
            }

            if (args.Length < 2)
            {
                var argFirst = args[0];
                if (argFirst == "-")
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

                if (TextOff.Equals(argFirst,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    PrintMatchedLine = (it) => PrintLineWithoutColor(it);
                    return;
                }

                if (argFirst.Equals("~"))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        HighlightForeColor = OldBackColor;
                        HighlightBackColor = OldForeColor;
                    }
                    else
                    {
                        var infoType = (typeThe == ArgType.CommandLine)
                        ? "Command line" : "Envir";
                        ConfigException.Add(typeThe, new ArgumentException(
                            $"{infoType}: Inverse to {opt.Name} is NOT supporte in the running OS."));
                    }
                    return;
                }

                (bool isBackground, string colorText) = (argFirst.StartsWith("~"), argFirst);
                if (isBackground)
                {
                    colorText = colorText[1..];
                }
                if (TryParseToForeColor(colorText, out var colorThe))
                {
                    if (isBackground)
                    {
                        HighlightBackColor = colorThe;
                    }
                    else
                    {
                        HighlightForeColor = colorThe;
                    }
                }
                else
                {
                    ConfigException.Add(typeThe, new
                        ArgumentException(
                        $"'{argFirst}' to {opt.Name} is NOT valid!"));
                }
                return;
            }

            bool isHighForeOk;
            bool isHighBackOk;
            ConsoleColor highFore;
            ConsoleColor highBack;
            switch (args[0].StartsWith("~"), args[1].StartsWith("~"))
            {
                case (true, false):
                    isHighBackOk = TryParseToForeColor(args[0][1..], out highBack);
                    isHighForeOk = TryParseToForeColor(args[1], out highFore);
                    break;
                case (false, true):
                    isHighBackOk = TryParseToForeColor(args[1][1..], out highBack);
                    isHighForeOk = TryParseToForeColor(args[0], out highFore);
                    break;
                default:
                    ConfigException.Add(typeThe, new ArgumentException(
                        $"Values '{args[0]}' and '{args[1]}' to {opt.Name} are NOT valid!"));
                    return;
            }
            if (isHighBackOk && isHighForeOk)
            {
                HighlightBackColor = highBack;
                HighlightForeColor = highFore;
            }
            else
            {
                ConfigException.Add(typeThe, new ArgumentException(
                    $"Values '{args[0]}' and '{args[1]}' to {opt.Name} are NOT valid!"));
            }
        });
    #endregion

    static public readonly IInvoke<Ignore, IConsolePause> PauseMaker =
        new SwitchInvoker<Ignore, IConsolePause>(TextPause,
            init: (_) => new ConsolePause(),
            alterFor: false,
            alter: (_) => new FakePause());
}
