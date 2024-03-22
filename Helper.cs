﻿using System.Reflection;
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

public record Match(int Index, int Length)
{
    public static readonly Match[] ZeroOne = [new Match(0, 0)];
}

public record MatchResult(int LineNumber, string Line, Match[] Matches);

internal static partial class Helper
{
    static public bool IsNumber(this char arg)
    {
        if ('0' <= arg && arg <= '9') return true;
        return false;
    }

    static public bool IsAlphaNumber(this char arg)
    {
        if ('0' <= arg && arg <= '9') return true;
        if ('A' <= arg && arg <= 'Z') return true;
        if ('a' <= arg && arg <= 'z') return true;
        return false;
    }

    static public IEnumerable<T> Invoke<T>(this IEnumerable<T> seq,
        Func<IEnumerable<T>, IEnumerable<T>> func)
    {
        return func(seq);
    }

    static public R Invoke<T, R>(this T seq, IInvoke<T, R> opt)
    {
        return opt.Invoke(seq);
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
        static IEnumerable<string> ReadLinesFrom(StreamReader inpFs)
        {
            string? lineThe;
            while (null != (lineThe = inpFs.ReadLine()))
            {
                yield return lineThe;
            }
            inpFs.Close();
        }

        if ("-" == path)
        {
            return ReadAllLinesFromConsole(option);
        }
        else
        {
            try
            {
                if (true != File.Exists(path))
                {
                    ConfigException.Add(ArgType.CommandLine,
                        new FileNotFoundException($"'{path}' to {option}"));
                    return [];
                }
                StreamReader inpFs = File.OpenText(path);
                return ReadLinesFrom(inpFs);
            }
            catch (Exception ee)
            {
                ConfigException.Add(ArgType.CommandLine, ee);
                return [];
            }
        }
    }

    public static bool IsEnvirDebug()
    {
        return Environment.GetEnvironmentVariable(nameof(grep))
            ?.Contains(DebugFlagText) ?? false;
    }

    record InfoShortcut(string Shortcut, string[] Expands)
    {
        public static readonly InfoShortcut Empty = new("", []);
    }

    public static bool PrintSyntax(bool isDetailed = false)
    {
        Console.WriteLine("Syntax:");
        if (false == isDetailed)
            Console.WriteLine($"  {nameof(grep)} -?");

        Console.WriteLine($"""
              {nameof(grep)} [OPTIONS] PATTERN [FILE [FILE ..]]

            """);

        if (false == isDetailed)
        {
            Console.WriteLine("""
                Read redir console input if FILE is -

                https://github.com/ck-yung/grep/blob/master/README.md
                """);
        }
        else
        {
            var bb = Options.Parsers
                .Select((it) => new KeyValuePair<string, EnvrParser>(it.Name, new(true, it)))
                .Union(Options.NonEnvirParsers
                .Select((it) => new KeyValuePair<string, EnvrParser>(it.Name, new(false, it))));
            var cc = Options.ShortCuts
                .Union(Options.NonEnvirShortCuts);
            var jj2 = bb.GroupJoin(cc, (b2) => b2.Key, (c2) => (c2.Value.Length > 0) ? c2.Value[0] : "?",
                (b2, cc2) => new
                {
                    Name = b2.Key,
                    EnvrParser = b2.Value,
                    Info = cc2.Any()
                    ? new InfoShortcut(cc2.First().Key, cc2.First().Value.Skip(1).ToArray())
                    : InfoShortcut.Empty,
                });

            Console.WriteLine("Shortcut           Option  with           Envir");
            foreach (var j2 in jj2
                .OrderBy((it) => it.EnvrParser.IsEnvir)
                .ThenBy((it) => it.Info.Shortcut))
            {
                var j3 = j2.Info;
                Console.Write($"{j2.Info.Shortcut,6}{j2.Name,19}  ");

                var a2 = j3.Expands.Length == 0
                    ? j2.EnvrParser.Parser.Help : j3.Expands[0];

                if (j2.EnvrParser.IsEnvir)
                {
                    Console.Write(a2);
                }
                else
                {
                    Console.Write($"{a2,-14} Command line only");
                }
                Console.WriteLine();
            }
            Console.WriteLine("""
                For example,
                  grep -nsm 3 class *.cs --color black,yellow -X obj
                  dir2 -sb *.cs --within 4hours | grep -n class -T -

                Options can be stored in envir var 'grep'.
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

    public static IEnumerable<string> FromWildCard(this string arg)
    {
        if (File.Exists(arg)) return [arg];
        if ("-" == arg) return ["-"];
        var a2 = Path.GetDirectoryName(arg);
        var pathThe = string.IsNullOrEmpty(a2) ? "." : a2;
        Func<string, string> getName = (pathThe == ".")
            ? (it) => it[2..] : Helper.Itself;
        var wildMatch = Dir.Wild.ToWildMatch(Path.GetFileName(arg));
        try
        {
            return Dir.Scan.SimpleListFiles(pathThe)
                .Where((it) =>
                {
                    var filename = Path.GetFileName(it);
                    return wildMatch(filename) &&
                    false == SubDir.ExclFile.Invoke(filename);
                })
                .Select((it) => getName(it));
                ;
        }
        catch (Exception ee)
        {
            Show.LogVerbose.Invoke($"File '{arg}' is NOT found!");
            Log.Debug("{0}>{1}>{2}", nameof(FromWildCard), arg, ee);
            return [];
        }
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

    public static void Debug(string[] arrayOfString, string format, params object[] args)
    {
        if (true != DebugFlag) return;
        string msg;
        msg = String.Format(format, args);
        if (arrayOfString?.Length > 0)
        {
            msg += " '" + string.Join(";", arrayOfString) + "'";
        }
        else
        {
            msg += format + " *empty-array*";
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
        MaxLineNumber = 2;
        Increase = FakeInc;
    }

    public void Auto()
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected)
        {
            Disable();
            return;
        }
        MaxLineNumber = Console.WindowHeight - 1;
        if (MaxLineNumber < 2)
        {
            MaxLineNumber = 2;
            Increase = FakeInc;
        }
        MaxLineLength = Console.WindowWidth - 1;
    }

    public bool Assign(int limit)
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected)
        {
            Disable();
            return true;
        }
        if (1 > limit) return false;
        MaxLineNumber = limit;
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

    int MaxLineNumber { get; set; } = int.MaxValue;
    int MaxLineLength { get; set; } = int.MaxValue;
    int Counter { get; set; } = 0;
    static int RealInc(int arg, int inc) => arg + inc;
    static int FakeInc(int _1, int _2) => 1;
    Func<int, int, int> Increase = (it2, it3) => RealInc(it2, it3);

    public void IncreaseCounter(int length)
    {
        int incNumber = 1;
        if (MaxLineLength > 0)
        {
            incNumber += length / MaxLineLength;
        }
        Counter = Increase(Counter, incNumber);
        if (Counter >= MaxLineNumber)
        {
            Console.ResetColor();
            Console.Write("Press any key (q to quit) ");
            var inp = Console.ReadKey();
            Console.Write("\r");
            //nsole.Write("Press any key (q to quit) 12");
            Console.Write("                            ");
            Console.Write("\r");
            if (inp.KeyChar == 'q' || inp.KeyChar == 'Q')
            {
                throw new ConfigException("Aborted");
            }
            Auto();
            Counter = 0;
        }
    }
}
