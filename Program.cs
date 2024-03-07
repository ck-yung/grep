using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;

namespace grep;

using static grep.Options;

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

    static bool PrintSyntax(bool isDetailed = false)
    {
        if (false == isDetailed)
            Console.WriteLine($"{nameof(grep)} -?");

        Console.WriteLine($"""
            {nameof(grep)} [OPTIONS] PATTERN [FILE [FILE ..]]
            """);

        if (false == isDetailed)
        {
            Console.WriteLine("""

                * grep does not support FILE in wild card.
                * PATTERN is a regular expression if it is NOT leading by a '~' char.
                * Read redir console input if no FILE is given.

                For example,
                  dir2 -sd | grep ~c++
                is same as
                  dir2 -sd | grep c\+\+
                """);
        }
        else
        {
            Console.WriteLine("""
                Short-cut  Option             With
                  -i       --case-sensitive   off
                  -w       --word             on
                  -n       --line-number      on
                  -c       --count            on
                  -l       --file-match       on
                  -v       --invert-match     on
                  -q       --quiet            on
                  -h       --show-filename    off
                  -p       --pause            off

                Short-cut  Option             Required
                  -m       --max-count        NUMBER
                  -T       --files-from       FILES-FROM
                  -f       --file             REGEX-FILE
                  -F       --fixed-text-file  FIXED-TEXT-FILE
                           --color            COLOR   or   ~COLOR

                Read redir console input if FILES-FROM is -
                Read redir console input if REGEX-FILE or FIXED-TEXT-FILE is -

                For example,
                  grep -inm 3 class -T cs-files.txt
                  dir2 -sb *cs | grep -inm 3 class -T -
                """);
        }
        return false;
    }

    static bool PrintVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var asmName = asm?.GetName();
        var nameThe = asmName?.Name ?? "?";
        var version = asmName?.Version?.ToString() ?? "???";
        var aa = asm?.GetCustomAttributes(
            typeof(AssemblyCopyrightAttribute),
            inherit: false);
        var copyright = "????";
        if ((aa!=null) && (aa.Length > 0))
        {
            copyright = ((AssemblyCopyrightAttribute)aa[0]).Copyright;
        }
        Console.WriteLine($"{nameThe}/C# v{version} {copyright}");
        return false;
    }

    const string OptFilesFrom = "--files-from";
    const string OptCaseSensitive = "--case-sensitive";
    const string OptWord = "--word";
    const string OptLineNumber = "--line-number";
    const string OptCountOnly = "--count";
    const string OptFileMatch = "--file-match";
    const string OptInvertMatch = "--invert-match";
    const string OptColor = "--color";
    const string OptWildFile = "--file";
    const string OptQuiet = "--quiet";
    const string OptMaxCount = "--max-count";
    const string OptShowFilename = "--show-filename";
    const string OptFixedTextFrom = "--fixed-text-file";
    const string OptPause = "--pause";

    static int PrintFilenameImpl(string filename)
    {
        Console.Write(filename+":");
        return 1 + filename.Length;
    }
    static Func<string, int> PrintFilename { get; set; }
        = (path) => PrintFilenameImpl(path);

    static bool CheckPathExists(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        if (File.Exists(path)) return true;
        Log.Verbose($"File '{path}' is NOT found.");
        return false;
    }

    static IEnumerable<string> ReadAllLinesFromConsole()
    {
        string? lineThe;
        while (null != (lineThe = Console.ReadLine()))
        {
            yield return lineThe;
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

        if (args.Length < 1) return PrintSyntax();

        var flagedArgs = args.ToFlagedArgs(new Dictionary<string, string>()
        {
            ["-T"] = OptFilesFrom,
            ["-f"] = OptWildFile,
            ["-m"] = OptMaxCount,
            ["-F"] = OptFixedTextFrom,
        }.ToImmutableDictionary(), new Dictionary<string, string[]>()
        {
            ["-i"] = [OptCaseSensitive, TextOff],
            ["-w"] = [OptWord, TextOn],
            ["-n"] = [OptLineNumber, TextOn],
            ["-c"] = [OptCountOnly, TextOn],
            ["-l"] = [OptFileMatch, TextOn],
            ["-v"] = [OptInvertMatch, TextOn],
            ["-q"] = [OptQuiet, TextOn],
            ["-h"] = [OptShowFilename, TextOff],
            ["-p"] = [OptPause, TextOff],
        }.ToImmutableDictionary());

        flagedArgs = ParseSwitch(flagedArgs, name: "--debug", when: TextOn,
            assert: false, parse: () => Log.DebugFlag = true);

        flagedArgs = ParseSwitch(flagedArgs, name: OptQuiet,
            when: TextOn, parse: () =>
            {
                Log.SwitchVerbose(enable: false);
            });

        (_, flagedArgs) = Parse<Ignore, Ignore>(flagedArgs,
            name: OptMaxCount, init: Ignore.Maker,
            parse: (numText) =>
            {
                if (0 == string.Compare("unlimit", numText,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return Ignore.Maker;
                }

                if (int.TryParse(numText, out var maxThe))
                {
                    if (maxThe < 1)
                    {
                        throw new ArgumentException(
                            $"The number to {OptMaxCount} should be greater than ZERO but {maxThe} is NOT!");
                    }
                    Counter.SetLimit(maxThe);
                }
                else
                {
                    throw new ArgumentException(
                        $"'{numText}' to {OptMaxCount} is NOT an number!");
                }
                return Ignore.Maker;
            });

        (PrintFilename, flagedArgs) = Parse<string, int>(flagedArgs,
            name: OptShowFilename, init: (path) => PrintFilenameImpl(path),
            parse: (flag) =>
            {
                if (CompareText(flag, TextOff))
                {
                    return (_) => 0;
                }
                AssertOption(OptQuiet, flag);
                return (path) => PrintFilenameImpl(path);
            });

        (var filesFrom, flagedArgs) = Parse<string[], IEnumerable<string>>(
            flagedArgs, name: OptFilesFrom,
            init: (paths) =>
            {
                if (paths.Length > 0)
                    return paths
                    .Select((it) => it.Trim())
                    .Where((it) => CheckPathExists(it))
                    .Distinct();
                if (Console.IsInputRedirected) return [""];
                throw new ArgumentException("No file is given.");
            },
            parse: (path) =>
            {
                if ("-" == path)
                {
                    if (true != Console.IsInputRedirected)
                    {
                        throw new ArgumentException(
                            $"{OptFilesFrom}: console input is NOT redirected!");
                    }
                    return (paths) => ReadAllLinesFromConsole()
                    .Union(paths)
                    .Select((it) => it.Trim())
                    .Where((it) => CheckPathExists(it))
                    .Distinct();
                }

                if (true != File.Exists(path))
                    throw new ArgumentException(
                        $"File '{path}' to {OptFilesFrom} is NOT found!");
                string? lineThe;
                var inpFs = File.OpenText(path);
                IEnumerable<string> FilesFromFile()
                {
                    while (null != (lineThe = inpFs.ReadLine()))
                    {
                        yield return lineThe;
                    }
                    inpFs.Close();
                }
                return (paths) => FilesFromFile()
                .Union(paths)
                .Select((it) => it.Trim())
                .Where((it) => CheckPathExists(it))
                .Distinct();
            });

        flagedArgs = ParseSwitch(flagedArgs, name: OptWord,
            when: TextOn, parse: () => Helper.RegexByWordPattern());

        flagedArgs = ParseSwitch(flagedArgs, name: OptCaseSensitive,
            when: TextOff, parse: () => Helper.WouldRegex(ignoreCase: true));

        flagedArgs = ParseSwitch(flagedArgs, name: OptLineNumber,
            when: TextOn, parse: () => Counter.EnablePrint(flag: true));

        (var postMatch, flagedArgs) = Parse<PostMatchParam, bool>(flagedArgs,
            name: OptCountOnly, init: (it) => DefaultPostMatch(it),
            parse: (flag) =>
            {
                if (CompareText(flag, TextOn))
                {
                    PrintFoundCount = (path, count) =>
                    {
                        if (1 > count) return;
                        Console.WriteLine($"{path}:{count}");
                    };
                    return (_) => true;
                };
                AssertOption(OptCountOnly, flag);
                return (it) => DefaultPostMatch(it);
            });

        (var checkMatches, flagedArgs) = Parse<MatchCollecton, bool>(flagedArgs,
            name: OptInvertMatch, init: (matches) => matches.Found,
            parse: (flag) =>
            {
                if (CompareText(flag, TextOn))
                {
                    DefaultPostMatch = (it) =>
                    {
                        // TODO: Check with PrintFilename
                        Console.WriteLine(it.Path + ":");

                        var rtn = Counter.Print(it.Index);
                        Console.WriteLine(it.Line);
                        // TODO: also return
                        // rtn.CounterOfCharPrinted +
                        // it.Path + 1 + it.Line
                        return rtn.IsContinuous;
                    };
                    return (matches) => true != matches.Found;
                }
                AssertOption(OptInvertMatch, flag);
                return (matches) => matches.Found;
            });

        ConsolePause.Auto();
        (_, flagedArgs) = Parse<Ignore, Ignore>(flagedArgs, name: OptPause,
            init: Ignore.Maker,
            parse: (argThe) =>
            {
                switch (argThe)
                {
                    case TextOff:
                        ConsolePause.Disable();
                        break;
                    case TextOn:
                        ConsolePause.Auto();
                        break;
                    default:
                        if (int.TryParse(argThe, out var iTmp))
                        {
                            if (true != ConsolePause.Assign(limit: iTmp))
                            {
                                throw new ArgumentException(
                                    $"{iTmp} is an invalid number to {OptPause}");
                            }
                        }
                        else
                        {
                            throw new ArgumentException(
                                $"{argThe} to {OptPause} is NOT 'on','off', or a number.");
                        }
                        break;
                }
                return Ignore.Maker;
            });

        flagedArgs = ParseSwitch(flagedArgs, name: OptFileMatch, when: TextOn,
            parse: () =>
            {
                var flagFound = false;
                postMatch = (_) =>
                {
                    flagFound = true;
                    return false;
                };
                PrintFoundCount = (path, count) =>
                {
                    if (flagFound)
                    {
                        if (string.IsNullOrEmpty(path))
                        {
                            Console.WriteLine("The input is matched to the regex.");
                        }
                        else
                        {
                            Console.WriteLine(path);
                            ConsolePause.Printed(path.Length);
                        }
                    }
                    flagFound = false;
                };
            });

        (var fixedTextFrom, flagedArgs) = Parse<Ignore, string[]>(
            flagedArgs, name: OptFixedTextFrom,
            init: (_) => [],
            parse: (path) =>
            {
                IEnumerable<string> GetLines()
                {
                    if ("-" == path)
                    {
                        if (true != Console.IsInputRedirected)
                        {
                            throw new ArgumentException(
                                $"Console input to {OptFixedTextFrom} is NOT redirected!");
                        }
                        return ReadAllLinesFromConsole();
                    }

                    IEnumerable<string> GetLinesFromTheFile()
                    {
                        if (true != File.Exists(path))
                            throw new ArgumentException(
                                $"File '{path}' to {OptFixedTextFrom} is NOT found!");
                        string? lineThe;
                        var inpFs = File.OpenText(path);
                        IEnumerable<string> FilesFromFile()
                        {
                            while (null != (lineThe = inpFs.ReadLine()))
                            {
                                yield return lineThe;
                            }
                            inpFs.Close();
                        }
                        return FilesFromFile();
                    }
                    return GetLinesFromTheFile();
                }

                var lines = GetLines()
                .Where((it) => false == string.IsNullOrWhiteSpace(it.Trim()))
                .Distinct()
                .ToArray();

                if (lines.Length == 0)
                {
                    if (path == "-") throw new ArgumentException(
                        $"Nothing is found in console input to {OptFixedTextFrom} of {nameof(grep)}.");
                    throw new ArgumentException(
                        $"Nothing is found in file '{path}' to {OptFixedTextFrom}.");
                }

                return (_) => lines;
            });

        (var generateMatchFunction, flagedArgs) = Parse<string[], WildFileResult>(
            flagedArgs, name: OptWildFile,
            init: (argsThe) =>
            {
                var fixedMatchs = fixedTextFrom(Ignore.Void);
                Func<string, MatchCollecton> matchFunc;
                switch (fixedMatchs.Length, argsThe.Length)
                {
                    case (0, 0):
                        Console.WriteLine("No Regex is given.");
                        throw new MissingValueException("");
                    case (0, _):
                        var patternThe = argsThe[0];
                        matchFunc = patternThe switch
                        {
                            "~" => Helper.MakeMatchingByFixedText("~"),
                            string it when it.StartsWith('~') =>
                                Helper.MakeMatchingByFixedText(patternThe[1..]),
                            _ => Helper.MakeMatchingByRegex(patternThe),
                        };
                        return new(argsThe.Skip(1).ToArray(), matchFunc);
                    default:
                        var matchsFunc = fixedMatchs
                        .Select((it) => Helper.MakeMatchingByFixedText(it))
                        .ToArray();
                        matchFunc = (textThe) => matchsFunc
                        .Select((funcThe) => funcThe(textThe))
                        .FirstOrDefault((resultThe) => resultThe.Found)
                        ?? MatchCollecton.Empty;
                        return new(argsThe, matchFunc);
                }
            },
            parse: (regexFrom) =>
            {
                string[] lines;
                if (regexFrom == "-")
                {
                    if (true != Console.IsInputRedirected)
                    {
                        throw new ArgumentException(
                            $"Console input is NOT redir but '-' is assigned to {OptWildFile}");
                    }
                    IEnumerable<string> ReadConsoleAllLines()
                    {
                        string? lineRead;
                        while (null != (lineRead = Console.ReadLine()))
                        {
                            yield return lineRead;
                        }
                    }
                    lines = ReadConsoleAllLines().ToArray();
                }
                else
                {
                    if (true != File.Exists(regexFrom))
                    {
                        throw new ArgumentException(
                            $"File '{regexFrom}' to {OptWildFile} is NOT found!");
                    }
                    lines = File.ReadAllLines(regexFrom);
                }

                var allWilds = lines
                .Select((it) => it.Trim())
                .Where((it) => it.Length > 0)
                .Distinct()
                .Select((it) => Helper.MakeMatchingByRegex(it))
                .Union(fixedTextFrom(Ignore.Void)
                    .Select((it) => Helper.MakeMatchingByFixedText(it)))
                .ToArray();

                if (allWilds.Length == 0)
                {
                    throw new ArgumentException(
                        $"Regex file '{regexFrom}' to {OptWildFile} is blank!");
                }

                MatchCollecton funcMatch(string it)
                {
                    var matchesFound = allWilds
                    .Select((it2) => it2(it))
                    .FirstOrDefault((it2) => it2.Found);

                    return matchesFound ?? MatchCollecton.Empty;
                }

                return (argsThe) =>
                {

                    return new(argsThe, (it) => funcMatch(it));
                };
            });

        (var initColor, flagedArgs) = ParseStrings<Ignore, Ignore>(flagedArgs,
            name: OptColor, init: (_) => ColorCfg.Init(["Red"]),
            parse: (flags) => (_) => ColorCfg.Init(flags),
            whenMissingValue: ColorCfg.Help);
        if (Console.IsOutputRedirected)
        {
            ColorCfg.Disable();
        }
        else
        {
            initColor(Ignore.Void);
        }

        (var argsRest, var matchFunc) = generateMatchFunction(
            flagedArgs.Select((it) => it.Arg).ToArray());

        var cntFileProcessed = 0;
        foreach (var filename in filesFrom(argsRest))
        {
            Log.Debug("File:'{0}'", filename);
            (var inpFs, var close, var printFilename) = OpenTextFile(filename);
            int cntLine = 0;
            int cntFound = 0;
            Counter.Reset();
            string? lineRead;
            while (null != (lineRead = inpFs.ReadLine()))
            {
                cntLine += 1;
                Log.Debug("{0}:'{1}'", cntLine, lineRead);
                var matches = matchFunc(lineRead);
                if (true != checkMatches(matches)) continue;
                cntFound += 1;
                if (true != postMatch(new(filename, cntLine, lineRead,
                    matches, printFilename)))
                {
                    break;
                }
            }
            close(inpFs);
            PrintFoundCount(filename, cntFound);
            cntFileProcessed += 1;
        }
        if (1 > cntFileProcessed)
        {
            Log.Verbose("No file is processed.");
        }
        return true;
    }

    static (StreamReader, Action<StreamReader>, Func<int> PrintFilename)
        OpenTextFile(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            return (new StreamReader(
                Console.OpenStandardInput()),
                (_) => { }, () => 0);
        }
        return (File.OpenText(filename), (it) => it.Close(),
            () => PrintFilename(filename));
    }

    static Action<string, int> PrintFoundCount { get; set; } = (_, _)  => { };
    record PostMatchParam(string Path, int Index, string Line,
        MatchCollecton Matches, Func<int> PrintFilename);

    static Func<PostMatchParam, bool> DefaultPostMatch { get; set; } = (param) =>
    {
        (var filename, var lineNbr, var lineRead,
        var matches, var printFilename) = param;
        var foundQueue = new List<(int, int)>();
        var lastIndex = 0;
        foreach (var match in matches.Matches)
        {
            foundQueue.Add((match.Index, match.Length));
            lastIndex = match.Index + match.Length;
        }
        var CounterOfCharPrinted = printFilename();
        var rtn = Counter.Print(lineNbr);
        CounterOfCharPrinted += rtn.CounterOfCharPrinted;
        var ndxLast = 0;
        foreach ((var ndx, var len) in foundQueue)
        {
            ColorCfg.Swith();
            switch (ndxLast, ndx)
            {
                case (0, 0): break;
                default:
                    Console.Write(lineRead[ndxLast..ndx]);
                    break;
            }
            ColorCfg.Swith();
            Console.Write(lineRead[ndx..(ndx + len)]);
            ndxLast = ndx + len;
        }
        Console.ResetColor();
        if (ndxLast < lineRead.Length)
        {
            Console.Write(lineRead[ndxLast..]);
        }
        ColorCfg.Reset();
        CounterOfCharPrinted += lineRead.Length;
        Console.WriteLine();
        ConsolePause.Printed(CounterOfCharPrinted);

        return rtn.IsContinuous;
    };
}
