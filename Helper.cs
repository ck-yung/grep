using System.Collections.Immutable;
using System.Reflection;
using RegX = System.Text.RegularExpressions;

namespace grep;

class Ignore
{
    public static Ignore Void { get; private set; } = new();
    public static Func<Ignore> Empty = () => Void;
    public static Func<Ignore, Ignore> Maker = (_) => Void;
    private Ignore() { }
}

class Ignore<T>
{
    public static Func<T, Ignore> Maker = (_) => Ignore.Void;
}

internal class MissingValueException: Exception
{
    public MissingValueException(string message) : base(message)
    { }
}

internal record Match(int Index, int Length);

internal record FlagedArg(bool Flag, string Arg);

internal static class Helper
{
    public const string TextOff = "off";
    public const string TextOn = "on";

    public static IEnumerable<FlagedArg> ToFlagedArgs(
        this IEnumerable<string> args,
        ImmutableDictionary<string, string[]> mapShortcuts)
    {
        var itrThe = args.GetEnumerator();
        IEnumerable<string> SeparateCombiningShortcut()
        {
            while (itrThe.MoveNext())
            {
                var current = itrThe.Current;
                if (current.StartsWith("--"))
                {
                    yield return current;
                }
                else if (current.StartsWith("-") && current.Length > 2)
                {
                    foreach (var charThe in current.Skip(1))
                    {
                        yield return "-" + charThe;
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }

        foreach (var arg in SeparateCombiningShortcut())
        {
            if (arg.Length == 2 && arg[0] == '-')
            {
                if (mapShortcuts.TryGetValue(arg, out var expands))
                {
                    foreach (var expand in expands)
                    {
                        yield return new(false, expand);
                    }
                }
                else
                {
                    yield return new(false, arg);
                }
            }
            else
            {
                yield return new(false, arg);
            }
        }
    }

    public static IEnumerable<string> ReadAllLinesFromConsole()
    {
        string? lineThe;
        while (null != (lineThe = Console.ReadLine()))
        {
            yield return lineThe;
        }
    }

    public static IEnumerable<string> ReadAllLinesFromFile(
        string path, string? option = null)
    {
        if ("-" == path)
        {
            foreach (var line in ReadAllLinesFromConsole())
                yield return line;
        }
        else
        {
            if (true != File.Exists(path))
            {
                if (string.IsNullOrEmpty(option))
                {
                    throw new ArgumentException(
                        $"File '{path}' is NOT found.");
                }
                throw new ArgumentException(
                    $"File '{path}' to {option} is NOT found.");
            }

            var inpFs = File.OpenText(path);
            string? lineThe;
            while (null != (lineThe = inpFs.ReadLine()))
            {
                yield return lineThe;
            }
            inpFs.Close();
        }
    }

    public static bool PrintSyntax(bool isDetailed = false)
    {
        if (false == isDetailed)
            Console.WriteLine($"{nameof(grep)} -?");

        Console.WriteLine($"""
            {nameof(grep)} [OPTIONS] PATTERN        [FILE [FILE ..]]
            {nameof(grep)} [OPTIONS] -f REGEX-FILE  [FILE [FILE ..]]
            """);

        if (false == isDetailed)
        {
            Console.WriteLine("""

                Read redir console input if no FILE is given.
                grep does not support FILE in wild card.
                
                PATTERN is a regular expression if it is NOT leading by a '^' char.
                For example,
                  dir2 -sd | grep ^c++
                is same as
                  dir2 -sd | grep c\+\+
                """);
        }
        else
        {
            Console.WriteLine("""
                Short-cut  Option             With
                  -c       --count            on
                  -h       --show-filename    off
                  -i       --case-sensitive   off
                  -l       --file-match       on
                  -n       --line-number      on
                  -p       --pause            off
                  -q       --quiet            on
                  -v       --invert-match     on
                  -w       --word             on
                
                Short-cut  Option             Required
                           --color            COLOR
                           --color           ^COLOR
                  -f       --regex-file       FILE
                  -F       --fixed-text-file  FILE
                  -m       --max-count        NUMBER
                  -T       --files-from       FILE

                Read redir console input if FILE is -

                For example,
                  grep -inm 3 class -T cs-files.txt
                  dir2 -sb *cs | grep -inm 3 class -T -
                """);
        }
        return false;
    }

    public static bool PrintVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var asmName = asm?.GetName();
        var nameThe = asmName?.Name ?? "?1";
        var version = asmName?.Version?.ToString() ?? "?2";
        var aa = asm?.GetCustomAttributes(
            typeof(AssemblyCopyrightAttribute),
            inherit: false);
        var copyright = "?3";
        if ((aa != null) && (aa.Length > 0))
        {
            copyright = ((AssemblyCopyrightAttribute)aa[0]).Copyright;
        }
        Console.WriteLine($"{nameThe}/C# v{version} {copyright}");
        return false;
    }

    public static void RegexByWordPattern()
    {
        //ToPattern = (it) => [
        //    "^" + it + @"\s",
        //    @"\s" + it + @"\s",
        //    @"\s" + it + "$",
        //];
    }

    public static RegX.Regex ToRegex(this string arg)
    {
        return new RegX.Regex(arg, RegX.RegexOptions.IgnoreCase);
    }
}

class Pattern
{
    public readonly Func<string, Match[]> Matches;

    public Pattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentNullException(nameof(Pattern));

        if (pattern.StartsWith('^'))
        {
            var tmp2 = (pattern.Length > 1) ? pattern[1..] : "^";
            Matches = (it) => it.Contains(tmp2) ? [new(0, 0)] : [];
        }
        else
        {
            Matches = (it) => new RegX.Regex(pattern)
            .Matches(it)
            .OfType<RegX.Match>()
            .Where((it) => it.Success)
            .Select((it) => new Match(it.Index, it.Length))
            .ToArray();
        }
    }
}

internal static class Log
{
    static Action<string> DebugWithString = (_) => { };
    static bool DebugFlagValue { get; set; }
    public static bool DebugFlag
    {
        get { return DebugFlagValue; }
        set
        {
            DebugFlagValue = value;
            if (DebugFlagValue)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    DebugWithString = (msg) =>
                    System.Diagnostics.Debug.WriteLine($"dbg: {msg}");
                }
                else
                {
                    DebugWithString = (msg) =>
                    Console.WriteLine($"dbg: {msg}");
                }
            }
        }
    }

    /// <param name="format">Debug prefix if args is string[];
    /// Otherwise, format string</param>
    public static void Debug(string format, params object[] args)
    {
        if (true != DebugFlag) return;
        string msg;
        if (args is string[] strings)
        {
            if (strings.Length == 0)
            {
                msg = format + " *empty-array*";
            }
            else
            {
                msg = format + "'" + string.Join(";", strings) + "'";
            }
        }
        else
        {
            msg = String.Format(format, args);
        }
        DebugWithString(msg);
    }

    static bool VerboseFlag_ = true;
    static public bool VerboseFlag
    {
        get => VerboseFlag_;
        set
        {
            VerboseFlag_ = value;
        }
    }

    public static Ignore Verbose(string format, params object[] args)
    {
        if (VerboseFlag_)
        {
            string msg = String.Format(format, args);
            Console.WriteLine(msg);
        }
        return Ignore.Void;
    }
}

internal static class ConsolePause
{
    public static void Disable()
    {
        IncreaseCounter = (_) => { };
    }

    public static void Auto()
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected)
        {
            Disable();
            return;
        }
        Limit = Console.WindowHeight - 1;
        if (Limit < 1)
        {
            IncreaseCounter = (_) => { };
        }
    }

    public static bool Assign(int limit)
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected)
        {
            Disable();
            return true;
        }
        if (1 > limit) return false;
        Limit = limit;
        return true;
    }

    /// <param name="length">Counter of char which has been printed.</param>
    public static void Printed(int length)
    {
        IncreaseCounter(length);
    }

    static int Limit { get; set; } = int.MaxValue;
    static int Counter { get; set; } = 0;

    // TODO: 'length' handling
    static Action<int> IncreaseCounter { get; set; } = (length) =>
    {
        Counter += 1;
        if (Counter >= Limit)
        {
            Console.Write("Press any key (q to quit) ");
            var inp = Console.ReadKey();
            Console.Write("\r");
            // sole.Write("Press any key (q to quit) AB");
            Console.Write("                             ");
            Console.Write("\r");
            if (inp.KeyChar == 'q' || inp.KeyChar == 'Q')
            {
                Console.ResetColor();
                Environment.Exit(0);
            }
            Counter = 0;
        }
    };
}
