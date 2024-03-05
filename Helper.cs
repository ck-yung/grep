﻿using REGEX = System.Text.RegularExpressions;

namespace grep;
internal record Match(int Index, int Length);

internal record MatchCollecton(bool Found, List<Match> Matches)
{
    static public readonly MatchCollecton Empty
        = new(false, new List<Match>());
}

internal static class Helper
{
    static Func<string, string[]> ToPattern = (it) => [it];
    public static Func<string, REGEX.Regex[]> MakeRegex { get; private set; } =
        (pattern) => ToPattern(pattern)
        .Select((it) => new REGEX.Regex(it))
        .ToArray();

    public static void RegexByWordPattern()
    {
        ToPattern = (it) => [
            "^" + it + @"\s",
            @"\s" + it + @"\s",
            @"\s" + it + "$",
        ];
    }

    public static void WouldRegex(bool ignoreCase)
    {
        if (ignoreCase)
        {
            MakeRegex = (pattern) => ToPattern(pattern)
            .Select((it) => new REGEX.Regex(it, REGEX.RegexOptions.IgnoreCase))
            .ToArray();
        }
    }

    public static MatchCollecton Matches(string fixedText, string text)
    {
        if (text.Contains(fixedText))
        {
            return new(true, [new(text.IndexOf(fixedText), fixedText.Length)]);
        }
        return MatchCollecton.Empty;
    }

    public static Func<string, MatchCollecton> MakeMatchingByFixedText(
        string fixedText)
    {
        return (text) => Matches(fixedText, text);
    }

    public static MatchCollecton Matches(REGEX.Regex[] regex, string text)
    {
        var result = regex.Select((it) => it.Matches(text))
            .Where((it) => it.Any())
            .ToArray();
        if (result.Length == 0) return MatchCollecton.Empty;
        return new(true, result
            .Select((coll) => coll.Select(
                (REGEX.Match it) => new Match(it.Index, it.Length)))
            .SelectMany((it) => (it))
            .OrderBy((it) => it.Index)
            .ToList());
    }

    public static Func<string, MatchCollecton> MakeMatchingByRegex(
        string pattern)
    {
        var regex = MakeRegex(pattern);
        return (text) => Matches(regex, text);
    }
}

public static class Log
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
                    DebugWithString = (msg) => System.Diagnostics.Debug.WriteLine($"dbg: {msg}");
                }
                else
                {
                    DebugWithString = (msg) => Console.WriteLine($"dbg: {msg}");
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
