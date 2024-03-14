using System.Collections.Immutable;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
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

    const string DebugFlagText = "--debug";
    static internal string CheckEnvirDebug(string name)
    {
        var envir = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(envir)) return String.Empty;
        if (envir.Contains(DebugFlagText))
        {
            Log.DebugFlag = true;
            Log.Debug("Debug log is enabled.");
            return envir.Replace(DebugFlagText, "");
        }
        return envir;
    }

    static internal IEnumerable<string> SplitEnvirVar(string value)
    {
        if (string.IsNullOrEmpty(value)) return [];
        return value.Split(' ', ';', '"')
            .Select((it) => it.Trim())
            .Where((it) => it.Length > 0);
    }

    static string? LastOptionReadConsole = null;
    public static IEnumerable<string> ReadAllLinesFromConsole(string option)
    {
        if (true != Console.IsInputRedirected)
        {
            throw new ConfigException(
                $"Console input is NOT redir but {option} -");
        }
        if (false == string.IsNullOrEmpty(LastOptionReadConsole))
        {
            throw new ConfigException(
                $"Options '{option}' and '{LastOptionReadConsole}' both read console input!");
        }
        LastOptionReadConsole = option;
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

    public static bool IsEnvirDebug()
    {
        return Environment.GetEnvironmentVariable(nameof(grep))
            ?.Contains(DebugFlagText) ?? false;
    }

    public static bool PrintSyntax(bool isDetailed = false)
    {
        Console.WriteLine("Syntax:");
        if (false == isDetailed)
            Console.WriteLine($"  {nameof(grep)} -?");

        Console.WriteLine($"""
              {nameof(grep)} [OPTIONS] PATTERN          [FILE [FILE ..]]
              {nameof(grep)} [OPTIONS] -f PATTERN-FILE  [FILE [FILE ..]]

            """);

        if (false == isDetailed)
        {
            Console.WriteLine("""
                Read redir console input if no FILE is -

                https://github.com/ck-yung/grep/blob/master/README.md
                """);
        }
        else
        {
            var bb = Options.Parsers
                .Select((it) => new KeyValuePair<string, EnvrParser>(it.Name, new(true, it)))
                .Union(Options.NonEnvirParsers
                .Select((it) => new KeyValuePair<string, EnvrParser>(it.Name, new(false, it))))
                .ToImmutableDictionary((it) => it.Key, (it) => it.Value);
            Console.WriteLine("Short-cut           Option  with            Envir");
            foreach ((var key, var strings) in Options.ShortCuts
                .Union(Options.NonEnvirShortCuts
                .Select((it) => new KeyValuePair<string, string[]>(it.Key, [it.Value])))
                .OrderBy((it) => it.Key))
            {
                Console.Write($"{key,4}   ");
                Console.Write($"{strings[0],19}  ");
                EnvrParser? found;
                switch (strings.Length > 1, bb.TryGetValue(strings[0], out found))
                {
                    case (true, true):
                        Console.Write($"{strings[1],-12} ");
                        if (true != (found?.IsEnvir ?? false))
                        {
                            Console.Write("   Not");
                        }
                        break;
                    case (false, true):
                        var help = found?.Parser?.Help ?? "";
                        Console.Write($"{help,-12} ");
                        if (true != (found?.IsEnvir ?? false))
                        {
                            Console.Write("   Not");
                        }
                        break;
                    default:
                        var a2 = string.Join(' ', strings.Skip(1));
                        Console.Write($"{a2}   ???");
                        break;
                }
                Console.WriteLine();
            }

            Console.WriteLine("""

                For example,
                  grep -nm 3 class *.cs
                  grep -ic class -T cs-files.txt
                  dir *.cs | grep -i Class -
                  dir2 -sb *.cs --within 4hours | grep -n class -T -
                """);
        }
        return false;
    }

    record EnvrParser(bool IsEnvir, IParse Parser);

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

    public static string[] FromWildCard(this string arg)
    {
        if (File.Exists(arg)) return [arg];
        if ("-" == arg) return ["-"];
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
        Matches = (line) => Options.MetaMatches.Invoke(
        Options.TextFindAllIndexOf(line, pattern)
        .Select((it) => new Match(it, pattern.Length)));
    }

    public Pattern(RegX.Regex regex)
    {
        Matches = (line) => Options.MetaMatches.Invoke(
            regex.Matches(line)
            .OfType<RegX.Match>()
            .Where(it => it.Success)
            .Select((it) => new Match(it.Index, it.Length)));
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
            //nsole.Write("Press any key (q to quit, c to cancel coming pause) 12");
            Console.Write("                                                      ");
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
