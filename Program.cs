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

            foreach (var ce2 in ConfigException.GetErrors())
            {
                Console.WriteLine(ce2);
            }
        }
        catch (ConfigException ce)
        {
            Console.WriteLine(ce.Message);
            Console.WriteLine($"Syntax: {nameof(grep)} -?");
        }
        catch (Exception ee)
        {
            Console.WriteLine(IsEnvirDebug()
                ? ee.ToString() : ee.Message);
        }
    }

    static bool RunMain(string[] args)
    {
        if (args.Length == 0) return PrintSyntax();

        if (args.Any((it) => it == "--version")) return PrintVersion();

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

        var filesMatched = paths
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
                return Show.FoundCount.Invoke(
                    new Show.CountFound(pause, path, cnt));
            })
            .GroupBy((it) => it)
            .ToImmutableDictionary((grp) => grp.Key,
            (grp) => grp.Count());

        switch (filesMatched.TryGetValue(true, out var cntFilesMatch),
            filesMatched.Count == 0)
        {
            case (true, _):
                if (cntFilesMatch == 1)
                    Show.LogVerbose.Invoke($"One files is matched.");
                else
                    Show.LogVerbose.Invoke($"{cntFilesMatch} files are matched.");
                break;
            case (false, true):
                throw new ConfigException("No file is given");
            default:
                Show.LogVerbose.Invoke($"No file is matched.");
                break;
        }
        return true;
    }
}
