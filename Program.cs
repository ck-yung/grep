﻿using System.Collections.Immutable;
using System.IO;

namespace grep;

using static grep.Helper;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            RunMain(args);
        }
        catch (ConfigException ce)
        {
            Console.ResetColor();
            if (0 < Console.GetCursorPosition().Left)
                Console.WriteLine();
            Console.WriteLine(ce.Message);
        }
        catch (Exception ee)
        {
            Console.ResetColor();
            if (0 < Console.GetCursorPosition().Left)
                Console.WriteLine();
            Console.WriteLine(IsEnvirDebug()
                ? ee.ToString() : ee.Message);
        }
    }

    static bool RunMain(string[] args)
    {
        var envUnknown = SplitEnvirVar(CheckEnvirDebug(nameof(grep)))
            .ToFlagedArgs(ArgType.Environment, Options.ShortCuts, [])
            .Resolve([])
            .Select((it) => it.Arg)
            .ToArray();

        if (args.Length == 0) return PrintSyntax();

        if (args.Any((it) => it == "--version")) return PrintVersion();

        var qry = from arg in args
                  join help in new string[] { "--help", "-?" }
                  on arg equals help into helpFound
                  from found in helpFound select found;
        if (qry.Any()) return PrintSyntax(isDetailed: true);

        args = args.ToFlagedArgs(ArgType.CommandLine,
            Options.ShortCuts, Options.NonEnvirShortCuts)
            .Resolve(Options.NonEnvirParsers)
            .Select((it) => it.Arg)
            .ToArray();

        (var matches, var paths) = Options.PatternsFrom.Invoke(args);

        if (Console.IsOutputRedirected)
        {
            ((IParse)Show.SwitchColor).Parse([
                new(true, ArgType.CommandLine, "off")]);
        }
        var pause = Show.PauseMaker.Invoke(Ignore.Void);

        var _ = paths
            .Select((it) => it.FromWildCard())
            .SelectMany((it) => it)
            .Union(Options.FilesFrom.Invoke(Ignore.Void))
            .Distinct()
            .Select((path) =>
            {
                var cnt = Options.ReadAllLinesFrom(path, option: "FILE")
                .Select((line, lineNumber)
                => new MatchResult(lineNumber, line, matches(line)))
                .Where((it) => it.Matches.Length > 0)
                .Invoke(Show.MaxFound.Invoke)
                .Select((it) =>
                {
                    var lenPrinted = Show.Filename.Invoke(path);
                    lenPrinted += Show.LineNumber.Invoke(it.LineNumber);
                    lenPrinted += Show.PrintMatchedLine(it);
                    pause.Printed(lenPrinted);
                    return it;
                })
                .Count();
                Show.TotalCount.Invoke(new Else<int, Ignore>(cnt));
                return Show.FoundCount.Invoke(
                    new Show.CountFound(pause, path, cnt));
            }).Count();

        Show.TotalCount.Invoke(new Else<int, Ignore>(Ignore.Void));

        if (envUnknown.Length > 0)
        {
            Show.LogVerbose.Invoke(
                $"Unknown envir: {string.Join(' ', envUnknown)}");
        }

        foreach (var infoError in ConfigException.GetErrors())
        {
            var envrThe = infoError.Type == ArgType.CommandLine
                ? "Command line" : "Envir";
            Show.LogVerbose.Invoke(
                $"{envrThe}: {infoError.Error.Message}");
            if (false == string.IsNullOrEmpty(infoError.Option?.ExtraHelp))
            {
                Show.LogVerbose.Invoke(infoError.Option.ExtraHelp);
            }
        }
        return true;
    }
}
