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
                $"'{string.Join(' ', envUnknown)}' is known to envir '{nameof(grep)}'.");
        }

        foreach (var infoError in ConfigException.GetErrors())
        {
            var envrThe = infoError.Type == ArgType.CommandLine
                ? "Command line" : $"Envir '{nameof(grep)}'";
            Show.LogVerbose.Invoke(
                $"{envrThe}: {infoError.Error.Message}");
            if (false == string.IsNullOrEmpty(infoError.Option?.ExtraHelp))
            {
                Show.LogVerbose.Invoke(infoError.Option.ExtraHelp);
            }
        }
    }

    static bool RunMain(string[] args)
    {
        if (args.Length == 0) return PrintSyntax();

        if (args.Any((it) => it == "--version")) return PrintVersion();

        if (args.Any((it) => it == "--help" || it == "-?"))
        {
            return PrintSyntax(isDetailed: true);
        }

        args = args.ToFlagedArgs(ArgType.CommandLine,
            Options.ShortCuts, Options.NonEnvirShortCuts)
            .Resolve(Options.NonEnvirParsers)
            .Select((it) => it.Arg)
            .ToArray();

        var pause = Show.PauseMaker.Invoke(Ignore.Void);
        var lineMarched = Show.PrintLineMaker.Invoke(Ignore.Void);
        var printer = Show.PrintMaker.Invoke(Ignore.Void);

        (var isPatternsFromRedir, var matches, var paths) =
            Options.PatternsFrom.Invoke(args);
        Log.Debug("RunMain paths='{0}'", string.Join(",",paths));

        var total = SubDir.FileScan.Invoke(paths)
            .Union(Options.FilesFrom.Invoke(
                new(isPatternsFromRedir, IsArgsEmpty: paths.Length == 0)))
            .Distinct()
            .Select((path) =>
            {
                Log.Debug($"Scan file '{path}'");

                var cntFinding = ReadAllLinesFromFile(path, option: "FILE")
                .Select((it) => Options.TrimStart.Invoke(it))
                .Select((line, lineNumber)
                => new MatchResult(lineNumber, line, matches(line)))
                .Where((it) => it.Matches.Length > 0)
                .Select((it) =>
                {
                    lineMarched.SetDefaultColor();
                    var lenPrinted = Show.Filename.Invoke(path);
                    lenPrinted += Show.LineNumber.Invoke(it.LineNumber);
                    lenPrinted += lineMarched.Print(it);
                    pause.Printed(lenPrinted);
                    return it.Matches.Length;
                })
                .Invoke(Show.TakeSumByMax);

                return printer.Print(new Show.PathFindingParam(path, cntFinding, pause));
            })
            .Aggregate(seed: new Show.PathFindingParam("", 0, pause), (acc, it) => acc.AddWith(it));

        printer.TotalPrint(Show.PrintTotal.Invoke(total));

        return true;
    }
}
