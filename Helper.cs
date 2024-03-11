using System.Collections.Generic;
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

class Always<T>
{
    public static Func<T, bool> True = (_) => true;
    public static Func<T, bool> Never = (_) => false;
}

internal class MissingValueException: Exception
{
    public MissingValueException(string message) : base(message)
    { }
}

public record Match(int Index, int Length)
{
    public static readonly Match[] ZeroOne = [new Match(0, 0)];
}

public record MatchResult(int LineNumber, string Line, Match[] Matches);

internal static partial class Helper
{
    static public IEnumerable<T> Invoke<T>(this IEnumerable<T> seq,
        Func<IEnumerable<T>, IEnumerable<T>> func)
    {
        return func(seq);
    }

    static public T Itself<T>(T arg) => arg;

    static internal IEnumerable<FlagedArg> GetEnvirOpts(string name)
    {
        var envirOld = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(envirOld)) return [];

        try
        {
            var aa = envirOld.Split("--")
                .Select((it) => it.Trim())
                .Select((it) => it.Trim(';'))
                .Where((it) => it.Length > 0)
                .Select((it) => "--" + it);

            return aa.ToFlagedArgs(ArgType.Environment,
                new Dictionary<string, string[]>() // TODO
                {
                }.ToImmutableDictionary());
        }
        catch (Exception ee)
        {
            ConfigException.Add(ArgType.Environment, name, ee);
            return [];
        }
    }

    public static IEnumerable<string> ReadAllLinesFromConsole(string option)
    {
        if (true != Console.IsInputRedirected)
        {
            throw new ArgumentException($"Console input for {option} is NOT redir!");
        }
        string? lineThe;
        while (null != (lineThe = Console.ReadLine()))
        {
            yield return lineThe;
        }
    }

    public static IEnumerable<string> ReadAllLinesFromFile(
        string path, string option)
    {
        if ("-" == path)
        {
            foreach (var line in ReadAllLinesFromConsole(option))
                yield return line;
        }
        else
        {
            if (true != File.Exists(path))
            {
                Show.LogVerbose.Invoke($"File '{path}' to {option} is NOT found.");
                yield break;
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
                
                PATTERN is a regular expression if it is NOT leading by a '~' char.
                For example,
                  dir2 -sd | grep ~c++
                is same as
                  dir2 -sd | grep c\+\+
                """);
        }
        else
        {
            Console.WriteLine("""
                Short-cut  Option             With
                  -c       --count-only       on
                  -F       --fixed-strings    on
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
                           --color           ~COLOR
                  -f       --regex-file       FILE
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

    public static string[] FromWildCard(this string arg)
    {
        if (File.Exists(arg)) return [arg];
        var a2 = Path.GetDirectoryName(arg);
        var pathThe = string.IsNullOrEmpty(a2) ? "." : a2;
        var wildThe = Path.GetFileName(arg) ?? ":";
        try
        {
            var aa = Directory.GetFiles(pathThe, searchPattern: wildThe);
            if (aa.Length > 0)
            {
                return aa
                    .Select((it) => it.StartsWith("." + Path.DirectorySeparatorChar)
                    ? it[2..] : it)
                    .ToArray();
            }
        }
        catch { }
        Show.LogVerbose.Invoke($"File '{arg}' is NOT found!");
        return [];
    }

    static private string Blank<T>(T _) { return ""; }
}

class Pattern
{
    public readonly Func<string, Match[]> Matches;

    public Pattern(string pattern)
    {
        var tmp3 = Options.MatchText(Options.TextContains);
        Matches = (it) => tmp3(it, pattern) ? Match.ZeroOne : [];
    }

    public Pattern(RegX.Regex regex)
    {
        var tmp5 = Options.MatchRegex.Invoke(regex);
        Matches = (it) => tmp5(it);
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
}

interface IConsolePause
{
    void Disable();
    void Auto();
    bool Assign(int limit);
    void Printed(int length);
    void IncreaseCounter(int length);
}

class FakePause: IConsolePause
{
    public void Disable() { }
    public void Auto() { }
    public bool Assign(int limit) { return false; }
    public void Printed(int length) { }
    public void IncreaseCounter(int length) { }
}

class ConsolePause : IConsolePause
{
    public ConsolePause()
    {
        Auto();
    }

    public void Disable()
    {
        Limit = 2;
        Increase = FakeInc;
    }

    public void Auto()
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected)
        {
            Disable();
            return;
        }
        Limit = Console.WindowHeight - 1;
        if (Limit < 2)
        {
            Limit = 2;
            Increase = FakeInc;
        }
    }

    public bool Assign(int limit)
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
    public void Printed(int length)
    {
        if (length> 0)
        {
            IncreaseCounter(length);
        }
    }

    int Limit { get; set; } = int.MaxValue;
    int Counter { get; set; } = 0;
    static int RealInc(int arg) => arg + 1;
    static int FakeInc(int _) => 1;
    Func<int, int> Increase = (it) => RealInc(it);

    // TODO: 'length' handling
    public void IncreaseCounter(int length)
    {
        Counter = Increase(Counter);
        if (Counter >= Limit)
        {
            Console.Write("Press any key (q to quit, c to cancel coming pause) ");
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
            Auto();
            Counter = 0;
            if (inp.KeyChar == 'c' || inp.KeyChar == 'C')
            {
                Disable();
            }
        }
    }
}
