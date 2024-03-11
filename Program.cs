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

        var cntFilesMatch = paths
            .Select((it) => it.FromWildCard())
            .SelectMany((it) => it)
            .Where((path) =>
            {
                var cnt = ReadAllLinesFromFile(path, option: "FILE")
                .Select((line, lineNumber) =>
                new MatchResult(lineNumber, line, matches(line)))
                .Where((it) => it.Matches.Length > 0)
                .Invoke(Show.MyTake.Invoke)
                .Select((it) =>
                {
                    Show.Filename.Invoke(path);
                    Show.LineNumber.Invoke(it.LineNumber);
                    Show.PrintMatchedLine(it.Line);
                    return it;
                })
                .Count();
                return Show.FoundCount.Invoke((path, cnt));
            })
            .Count();

        switch (cntFilesMatch)
        {
            case 0:
                Show.LogVerbose.Invoke($"No file is matched.");
                break;
            case 1:
                Show.LogVerbose.Invoke($"One files is matched.");
                break;
            default:
                Show.LogVerbose.Invoke($"{cntFilesMatch} files are matched.");
                break;
        }
        return true;
    }
}
