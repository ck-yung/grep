using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;

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
        Console.WriteLine(isDetailed
            ? $"{nameof(grep)} -?"
            : $"{nameof(grep)} --help");
        Console.WriteLine($"""
            {nameof(grep)} [OPTIONS] REGEX [FILE [FILE ..]]
            """);
        if (isDetailed)
        {
            Console.WriteLine($"""

            OPTIONS:            DEFAULT  ALTERATIVE
              --verbose         off      on
              --case-sensitive  on       off
              --word            off      on
              --line-number     off      on
              --count           off      on
              --file-match      off      on
              --invert-match    off      on
              --files-from               FILES-FROM
              --file                     REGEX-FILE

            Short-Cut
              -T       --files-from
              -f       --file
              -i       --case-sensitive off
              -w       --word on
              -n       --line-number on
              -c       --count on
              -l       --file-match on
              -v       --invert-match on

            Read redir console input if FILES-FROM or FILE is -
            Read console keyboard input if FILES-FROM is --
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
        Console.WriteLine($"{nameThe}/C# {version} {copyright}");
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

    static bool RunMain(string[] args)
    {
        if (args.Any((it) => it == "--help"))
            return PrintSyntax(true);
        if (args.Any((it) => it == "--version"))
            return PrintVersion();
        if (args.Length < 1) return PrintSyntax();

        var qry = from arg in args
                  join help in new string[] { "-?", "-h"}
                  on arg equals help into helpQuery
                  from found in helpQuery select found;
        if (qry.Any()) return PrintSyntax();

        var flagedArgs = args.ToFlagedArgs(new Dictionary<string, string>()
        {
            ["-T"] = OptFilesFrom,
            ["-f"] = OptWildFile,
        }.ToImmutableDictionary(), new Dictionary<string, string[]>()
        {
            ["-i"] = [OptCaseSensitive, "off"],
            ["-w"] = [OptWord, "on"],
            ["-n"] = [OptLineNumber, "on"],
            ["-c"] = [OptCountOnly, "on"],
            ["-l"] = [OptFileMatch, "on"],
            ["-v"] = [OptInvertMatch, "on"],
        }.ToImmutableDictionary());

        (var logDebug, flagedArgs) = Parse<string, bool>(flagedArgs,
            name: "--debug", init: (_) => false,
            parse: (flag) =>
            {
                if (flag == "on")
                {
                    return (msg) =>
                    {
                        Console.WriteLine($"dbg: {msg}");
                        return true;
                    };
                }
                return (_) => false;
            });

        (var filesFrom, flagedArgs) = Parse<bool, IEnumerable<string>>(flagedArgs,
            name: OptFilesFrom, init: (_) => Array.Empty<string>(),
            parse: (path) =>
            {
                string? lineThe;
                if ("-" == path)
                {
                    if (true != Console.IsInputRedirected)
                    {
                        throw new ArgumentException(
                            "Console input is NOT redirected!");
                    }
                    IEnumerable<string> PathsFromConsole()
                    {
                        while (null != (lineThe = Console.ReadLine()))
                        {
                            yield return lineThe;
                        }
                    }
                    return (_) => PathsFromConsole();
                }

                if ("--" == path)
                {
                    if (true == Console.IsInputRedirected)
                    {
                        throw new ArgumentException(
                            "Console input is redirected but -- is assigned to --files-from!");
                    }
                    IEnumerable<string> PathsFromConsole()
                    {
                        while (null != (lineThe = Console.ReadLine()))
                        {
                            yield return lineThe;
                        }
                    }
                    return (_) => PathsFromConsole();
                }

                if (true != File.Exists(path))
                    throw new ArgumentException($"'{path}' to {OptFilesFrom} is NOT found!");
                var inpFs = File.OpenText(path);
                IEnumerable<string> FilesFromFile()
                {
                    while (null != (lineThe = inpFs.ReadLine()))
                    {
                        yield return lineThe;
                    }
                    inpFs.Close();
                }
                return (_) => FilesFromFile();
            });

        (var toWord, flagedArgs) = Parse<string, string>(flagedArgs,
            name: OptWord, init: (it) => it,
            parse: (flag) =>
            {
                if (flag == "on") return (it) => @"\s" + it + @"\s";
                return (it) => it;
            });

        (var makeRegex, flagedArgs) = Parse<string, Regex>(flagedArgs,
            name: OptCaseSensitive, init: (it) => new Regex(it),
            parse: (flag) =>
            {
                if (flag == "off") return (it) =>
                new Regex(it, RegexOptions.IgnoreCase);
                return (it) => new Regex(it);
            });

        (var _, flagedArgs) = Parse<bool, bool>(flagedArgs,
            name: OptLineNumber, init: (_) => true,
            parse: (flag) =>
            {
                if (flag == "on")
                {
                    PrintLineNumber = (it) => Console.Write($"{it}:");
                    return (_) => true;
                };
                return (_) => true;
            });

        (var postMatch, flagedArgs) = Parse<PostMatchParam, bool>(flagedArgs,
            name: OptCountOnly, init: (it) => DefaultPostMatch(it),
            parse: (flag) =>
            {
                if (flag == "on")
                {
                    PrintFoundCount = (path, count) =>
                    {
                        if (1 > count) return;
                        Console.WriteLine($"{path}:{count}");
                    };
                    return (_) => true;
                };
                return (it) => DefaultPostMatch(it);
            });

        (var checkMatches, flagedArgs) = Parse<MatchCollection, bool>(flagedArgs,
            name: OptInvertMatch, init: (matches) => matches.Any(),
            parse: (flag) =>
            {
                if (flag == "on")
                {
                    DefaultPostMatch = (it) =>
                    {
                        Console.WriteLine(it.Path + ":");
                        PrintLineNumber(it.Index);
                        Console.WriteLine(it.Line);
                        return true;
                    };
                    return (matches) => true != matches.Any();
                }
                return (matches) => matches.Any();
            });

        (var _, flagedArgs) = Parse<bool, bool>(flagedArgs,
            name: OptFileMatch, init: (_) => true,
            parse: (flag) =>
            {
                if (flag == "on")
                {
                    var flagFound = false;
                    postMatch = (_) =>
                    {
                        flagFound = true;
                        return false;
                    };
                    PrintFoundCount = (path, count) =>
                    {
                        if (flagFound) Console.WriteLine(path);
                        flagFound = false;
                    };
                };
                return (_) => true;
            });

        (var generateRegex, flagedArgs) = Parse<string[], WildFileResult>(
            flagedArgs, name: OptWildFile,
            init: (argsThe) =>
            {
                if (argsThe.Length == 0)
                {
                    Console.WriteLine("No Regex is found.");
                    throw new MissingValueException("");
                }
                logDebug($"< argsThe='{string.Join(";", argsThe)}'");
                var regEx = makeRegex(toWord(argsThe[0]));
                logDebug($"Regex('{argsThe[0]}') => '{regEx}'");
                argsThe = argsThe.Skip(1).ToArray();
                logDebug($"> argsThe='{string.Join(";", argsThe)}'");
                return new(argsThe, (it) => regEx.Matches(it));
            },
            parse: (path) =>
            {
                string[] lines;
                if (path == "-")
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
                    if (true != File.Exists(path))
                    {
                        throw new ArgumentException(
                            $"File '{path}' to {OptWildFile} is NOT found!");
                    }
                    lines = File.ReadAllLines(path);
                }

                var fakeWild = new Regex("a");
                var falseMatches = fakeWild.Matches("z");
                var allWilds = lines
                .Select((it) => it.Trim())
                .Where((it) => it.Length > 0)
                .Distinct()
                .Select((it) => makeRegex(toWord(it)))
                .ToArray();

                if (allWilds.Length == 0)
                {
                    throw new ArgumentException(
                        $"File '{path}' to {OptWildFile} is EMPTY!");
                }

                Func<string, MatchCollection> funcThe = (it) =>
                {
                    foreach (var wild in allWilds)
                    {
                        var matchResult = wild.Matches(it);
                        if (matchResult.Any()) return matchResult;
                    }
                    return falseMatches;
                };

                return (argsThe) => new(argsThe, funcThe);
            });

        (var initColor, flagedArgs) = Parse<bool, bool>(flagedArgs,
            name: OptColor, init: (_) => ForeColor.Init("Red"),
            parse: (flag) => (_) => ForeColor.Init(flag),
            whenMissingValue: ForeColor.Help);
        if (Console.IsOutputRedirected)
        {
            ForeColor.Disable();
        }
        else
        {
            initColor(true);
        }

        (var argsRest, var matchFunc) = generateRegex(
            flagedArgs.Select((it) => it.Arg).ToArray());

        foreach (var filename in filesFrom(true)
            .Select((it) => it.Trim())
            .Where((it) => File.Exists(it))
            .Union(
            argsRest
            .Where((it) => File.Exists(it) || it == "-"))
            .Distinct())
        {
            logDebug($"File:'{filename}'");
            (var inpFs, var close) = OpenTextFile(filename);
            int cntLine = 0;
            int cntFound = 0;
            string? lineRead;
            while (null != (lineRead = inpFs.ReadLine()))
            {
                cntLine += 1;
                logDebug($"{cntLine}:'{lineRead}'");
                var matches = matchFunc(lineRead);
                if (true != checkMatches(matches)) continue;
                cntFound += 1;
                if (true != postMatch(new(filename, cntLine, lineRead, matches)))
                {
                    break;
                }
            }
            close(inpFs);
            PrintFoundCount(filename, cntFound);
        }
        return true;
    }

    static (StreamReader, Action<StreamReader>) OpenTextFile(string filename)
    {
        if (filename == "-")
        {
            if (true != Console.IsInputRedirected)
            {
                throw new ArgumentException(
                    "Console input is NOT redir but FILE '-' is found");
            }
            return (new StreamReader(
                Console.OpenStandardInput()), (_) => { });
        }
        return (File.OpenText(filename), (it) => it.Close());
    }

    static Action<int> PrintLineNumber { get; set; } = (_) => { };
    static Action<string, int> PrintFoundCount { get; set; } = (_, _)  => { };
    record PostMatchParam(string Path, int Index, string Line, MatchCollection Matches);

    static Func<PostMatchParam, bool> DefaultPostMatch { get; set; } = (param) =>
    {
        (var filename, var lineNbr, var lineRead, var matches) = param;
        var foundQueue = new List<(int, int)>();
        var lastIndex = 0;
        foreach (Match match in matches)
        {
            foundQueue.Add((match.Index, match.Length));
            lastIndex = match.Index + match.Length;
        }
        Console.Write($"{filename}:");
        PrintLineNumber(lineNbr);
        var ndxLast = 0;
        foreach ((var ndx, var len) in foundQueue)
        {
            ForeColor.Swith();
            switch (ndxLast, ndx)
            {
                case (0, 0): break;
                default:
                    Console.Write(lineRead[ndxLast..ndx]);
                    break;
            }
            ForeColor.Swith();
            Console.Write(lineRead[ndx..(ndx + len)]);
            ndxLast = ndx + len;
        }
        Console.ResetColor();
        if (ndxLast < lineRead.Length)
        {
            Console.Write(lineRead[ndxLast..]);
        }
        ForeColor.Reset();
        Console.WriteLine();
        return true;
    };
}
