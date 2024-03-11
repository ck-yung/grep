using System.Collections.Immutable;
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
        catch (MissingValueException mve)
        {
            if (false == string.IsNullOrEmpty(mve.Message))
            {
                Console.WriteLine(mve.Message);
            }
        }
        catch (ArgumentException ae)
        {
            Console.WriteLine(ae.Message);
        }
        catch (Exception ee)
        {
            Console.WriteLine(ee);
        }
    }

    static bool RunMain(string[] args)
    {
        if (args.Any((it) => it == "--version"))
            return PrintVersion();

        var qry = from arg in args
                  join help in new string[] { "--help", "-?" }
                  on arg equals help into helpFound
                  from found in helpFound select found;
        if (qry.Any()) return PrintSyntax(isDetailed: true);

        args = args
            .ToFlagedArgs(ArgType.CommandLine, Options.ExpandStrings)
            .Resolve()
            .Select((it) => it.Arg)
            .ToArray();

        (var matches, var paths) = Options.PatternsFrom.Invoke(args);

        var pause = Show.PauseMaker.Invoke(Ignore.Void);

        var mapMatched = paths
            .Select((it) => it.FromWildCard())
            .SelectMany((it) => it)
            .Union(Options.FilesFrom.Invoke(Ignore.Void))
            .Distinct()
            .Select((path) =>
            {
                var cnt = ReadAllLinesFromFile(path, option: "FILE")
                .Select((line, lineNumber) =>
                new MatchResult(lineNumber, line, matches(line)))
                .Where((it) => it.Matches.Length > 0)
                .Invoke(Show.MyTake.Invoke)
                .Select((it) =>
                {
                    var lenPrinted = Show.Filename.Invoke(path);
                    lenPrinted += Show.LineNumber.Invoke(it.LineNumber);
                    lenPrinted += Show.PrintMatchedLine(it.Line);
                    pause.Printed(lenPrinted);
                    return it;
                })
                .Count();
                return Show.FoundCount.Invoke((pause, path, cnt));
            })
            .GroupBy((it) => it)
            .ToImmutableDictionary((grp) => grp.Key,
            (grp) => grp.Count());

        if (mapMatched.TryGetValue(true, out var cntFilesMatch))
        {
            if (cntFilesMatch == 1)
                Show.LogVerbose.Invoke($"One files is matched.");
            else
                Show.LogVerbose.Invoke($"{cntFilesMatch} files are matched.");
        }
        else
        {
            if (mapMatched.ContainsKey(false))
            {
                Show.LogVerbose.Invoke($"No file is matched.");
            }
            else
            {
                PrintSyntax();
            }
        }
        return true;
    }
}
