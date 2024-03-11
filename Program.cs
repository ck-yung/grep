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

        (var patternText, var paths) = GetRegexPaths(args);

        if (string.IsNullOrEmpty(patternText))
        {
            PrintSyntax();
            return false;
        }

        var patternThe = Options.ToPattern.Invoke(patternText);
        if (paths.Length == 0)
        {
            if (true != Console.IsInputRedirected)
            {
                PrintSyntax();
                return false;
            }

            var cnt = ReadAllLinesFromConsole()
                .Select((line, lineNumber) =>
                new MatchResult(lineNumber, line, patternThe.Matches(line)))
                .Where((it) => it.Matches.Length > 0)
                .Invoke(Show.MyTake.Invoke)
                .Select(it =>
                {
                    Show.LineNumber.Invoke(it.LineNumber);
                    Show.PrintMatchedLine(it.Line);
                    return it;
                })
                .Count();
            Show.FoundCount.Invoke(("", cnt));
        }
        else
        {
            foreach (var path in paths
                .Select((it) => it.FromWildCard())
                .SelectMany((it) => it))
            {
                var cnt = ReadAllLinesFromFile(path, option: "FILE")
                    .Select((line, lineNumber) =>
                    new MatchResult(lineNumber, line, patternThe.Matches(line)))
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
                Show.FoundCount.Invoke((path, cnt));
            }
        }
        return true;
    }

    static (string, string[]) GetRegexPaths(string[] paths)
    {
        switch (paths.Length)
        {
            case 0:
                return ("", []);
            case 1:
                return (paths[0], []);
            default:
                return (paths[0], paths.Skip(1).ToArray());
        }
    }
}
