namespace grep;

using static grep.Helper;

class Program
{
    static void Main(string[] args)
    {
        string[] envUnknown = [];
        try
        {
            try
            {
                envUnknown = SplitEnvirVar(CheckEnvirDebug(nameof(grep)))
                    .ToFlagedArgs(ArgType.Environment, Options.ShortCuts, [])
                    .Resolve([])
                    .Select((it) => it.Arg)
                    .ToArray();
            }
            catch (Exception ee)
            {
                ConfigException.Add(ArgType.Environment, ee);
            }

            RunMain(args);
        }
        catch (NoMessageException) { }
        catch (ConfigException ce)
        {
            Console.ResetColor();
            if (0 < Console.GetCursorPosition().Left)
                Console.WriteLine();
            if (false == string.IsNullOrWhiteSpace(ce.Message))
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

        if (envUnknown.Length > 0)
        {
            Show.LogVerbose.Invoke(
                $"'{string.Join(' ', envUnknown)}' is unknown to envir '{nameof(grep)}'.");
        }

        ConfigException.PrintErrors();
    }

    static bool RunMain(string[] args)
    {
        args = Options.SkipArgs.Invoke(args).ToArray();
        if (args.Length == 0) return PrintSyntax();

        if (args.Any((it) => it == "--version")) return PrintVersion();

        if (args.Any((it) => it == "--help" || it == "-?"))
        {
            return PrintSyntax(isDetailed: true);
        }

        if (args.Any((it) => it == "-??"))
        {
            return PrintSyntax(isDetailed: true, isShortHelp: false);
        }

        args = args.ToFlagedArgs(ArgType.CommandLine,
            Options.ShortCuts, Options.NonEnvirShortCuts)
            .Resolve(Options.NonEnvirParsers)
            .Select((it) => it.Arg)
            .ToArray();

        var pause = Show.MatchedFilenameWithCount.Invoke(
            Show.MatchedFilenameOnly.Invoke(
            Show.PauseMaker.Invoke(Ignore.Void)));

        var lineMarched = Show.PrintLineMaker.Invoke(Ignore.Void);

        (var isPatternsFromRedir, var matches, var paths) =
            Options.PatternsFrom.Invoke(args);
        Log.Debug(paths, nameof(RunMain));

        var showFilename = Show.Filename.Invoke(pause);

        var infoTotal = SubDir.FileScan.Invoke((ArgType.CommandLine,
            Options.SplitFileByComma.Invoke(paths)))
            .Union(Options.FilesFrom.Invoke(
                new(isPatternsFromRedir, IsArgsEmpty: paths.Length == 0)))
            .Distinct(Options.FilenameCaseSentive.Invoke(Ignore.Void))
            .Select((path) =>
            {
                showFilename.Assign(path);
                var cntFinding = ReadAllLinesFromFile(path, option: "FILES-FROM")
                .Select((it) => Options.TrimLine.Invoke(it))
                .Select((line, lineNumber)
                => new MatchResult(lineNumber, line, matches(line)))
                .Where((it) => it.Matches.Length > 0)
                .Select((it) =>
                {
                    lineMarched.SetDefaultColor();
                    var lenPrinted = showFilename.Print();
                    lenPrinted += Show.LineNumber.Invoke(it.LineNumber);
                    lenPrinted += lineMarched.Print(it);
                    pause.Printed(lenPrinted);
                    return it.Matches.Length;
                })
                .Invoke(Show.TakeSumByMax);
                showFilename.PrintFooter();
                return Show.FindingReport.Make(path, cntFinding);
            })
            .Aggregate(seed: new Show.InfoTotal(),
            (acc, it) => acc.AddWith(it));

        Show.PrintTotal.Invoke(infoTotal);

        return true;
    }
}
