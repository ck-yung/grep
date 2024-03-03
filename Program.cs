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

    static bool LogDebugFlag { get; set; } = false;

    static void LogDebug(string format, params object[] args)
    {
        if (LogDebugFlag)
        {
            var msg = String.Format(format, args);
            Console.WriteLine($"dbg: {msg}");
        }
    }

    static void LogDebug(string format, string[] args)
    {
        if (LogDebugFlag)
        {
            var msg = String.Format(format, string.Join(";", args));
            Console.WriteLine($"dbg: {msg}");
        }
    }

    static bool PrintSyntax(bool isDetailed = false)
    {
        if (false == isDetailed)
            Console.WriteLine($"{nameof(grep)} -?");

        Console.WriteLine($"""
            {nameof(grep)} [OPTIONS] REGEX [FILE [FILE ..]]
            """);
        if (isDetailed)
        {
            Console.WriteLine($"""

            OPTIONS:            DEFAULT  ALTERATIVE
              --color           RED      COLOR
              --case-sensitive  on       off
              --word            off      on
              --line-number     off      on
              --count           off      on
              --file-match      off      on
              --invert-match    off      on
              --quiet           off      on
              --max-count       UNLIMIT  NUMBER
              --files-from               FILES-FROM
              --file                     REGEX-FILE

            Short-Cut
              -i       --case-sensitive off
              -w       --word on
              -n       --line-number on
              -c       --count on
              -l       --file-match on
              -v       --invert-match on
              -q       --quiet on
              -m       --max-count
              -T       --files-from
              -f       --file

            Read redir console input if FILES-FROM or REGEX-FILE is -
            Read redir console input if no FILE is given.
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

    static bool LogVerboseImpl(string msg)
    {
        Console.WriteLine(msg);
        return true;
    }
    static Func<string, bool> LogVerbose { get; set; }
        = (msg) => LogVerboseImpl(msg);

    static bool CheckPathExists(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        if (File.Exists(path)) return true;
        LogVerbose($"File '{path}' is NOT found.");
        return false;
    }

    static bool RunMain(string[] args)
    {
        if (args.Any((it) => it == "-?"))
            return PrintSyntax(true);
        if (args.Any((it) => it == "--version"))
            return PrintVersion();
        if (args.Length < 1) return PrintSyntax();

        var qry = from arg in args
                  join help in new string[] { "--help", "-h" }
                  on arg equals help into helpQuery
                  from found in helpQuery select found;
        if (qry.Any()) return PrintSyntax();

        var flagedArgs = args.ToFlagedArgs(new Dictionary<string, string>()
        {
            ["-T"] = OptFilesFrom,
            ["-f"] = OptWildFile,
            ["-m"] = OptMaxCount,
        }.ToImmutableDictionary(), new Dictionary<string, string[]>()
        {
            ["-i"] = [OptCaseSensitive, "off"],
            ["-w"] = [OptWord, "on"],
            ["-n"] = [OptLineNumber, "on"],
            ["-c"] = [OptCountOnly, "on"],
            ["-l"] = [OptFileMatch, "on"],
            ["-v"] = [OptInvertMatch, "on"],
            ["-q"] = [OptQuiet, "on"],
        }.ToImmutableDictionary());

        (var _, flagedArgs) = Parse<string, bool>(flagedArgs,
            name: "--debug", init: (_) => false,
            parse: (flag) =>
            {
                if (flag == "on")
                {
                    LogDebugFlag = true;
                }
                return (_) => false;
            });

        (LogVerbose, flagedArgs) = Parse<string, bool>(flagedArgs,
            name: OptQuiet, init: (msg) => LogVerboseImpl(msg),
            parse: (flag) =>
            {
                if (flag == "on")
                {
                    return Never<string>.Holds;
                }
                return (msg) => LogVerboseImpl(msg);
            });

        (_, flagedArgs) = Parse<bool, bool>(flagedArgs,
            name: OptMaxCount, init: Always<bool>.True,
            parse: (numThe) =>
            {
                if (int.TryParse(numThe, out var maxThe))
                {
                    if (maxThe < 1)
                    {
                        throw new ArgumentException(
                            $"{maxThe} to {OptMaxCount} is an invalid number!");
                    }
                    Counter.SetLimit(maxThe);
                }
                else
                {
                    throw new ArgumentException(
                        $"'{numThe}' to {OptMaxCount} is NOT an number!");
                }
                return Always<bool>.True;
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
                    return (paths) => PathsFromConsole()
                    .Union(paths)
                    .Select((it) => it.Trim())
                    .Where((it) => CheckPathExists(it))
                    .Distinct();
                }

                if (true != File.Exists(path))
                    throw new ArgumentException(
                        $"File '{path}' to {OptFilesFrom} is NOT found!");
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
            name: OptLineNumber, init: Always<bool>.True,
            parse: (flag) =>
            {
                if (flag == "on")
                {
                    Counter.EnablePrint(flag: true);
                    //PrintLineNumber = (it) => Console.Write($"{it}:"); ** TODO
                    return (_) => true;
                };
                return Always<bool>.True;
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
                        //PrintLineNumber(it.Index);
                        var rtn = Counter.Print(it.Index);
                        Console.WriteLine(it.Line);
                        return rtn;
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
                        if (flagFound)
                        {
                            if (string.IsNullOrEmpty(path))
                            {
                                Console.WriteLine("The input is matched to the regex.");
                            }
                            else
                            {
                                Console.WriteLine(path);
                            }
                        }
                        flagFound = false;
                    };
                };
                return Always<bool>.True;
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
                LogDebug("< argsThe='{0}'", argsThe);
                var regEx = makeRegex(toWord(argsThe[0]));
                LogDebug("Regex('{0}') => '{1}'", argsThe[0], regEx);
                argsThe = argsThe.Skip(1).ToArray();
                LogDebug("> argsThe='{0}'", argsThe);
                return new(argsThe, (it) => regEx.Matches(it));
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
                .Select((it) => makeRegex(toWord(it)))
                .ToArray();

                if (allWilds.Length == 0)
                {
                    throw new ArgumentException(
                        $"Regex file '{regexFrom}' to {OptWildFile} is blank!");
                }

                var fakeWild = new Regex("a");
                var falseMatches = fakeWild.Matches("z");
                Func<string, MatchCollection> funcMatch = (it) =>
                {
                    var matchesFound = allWilds
                    .Select((it2) => it2.Matches(it))
                    .FirstOrDefault((it2) => it2.Any());
                    return matchesFound ?? falseMatches;
                };

                return (argsThe) => new(argsThe, funcMatch);
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

        var cntFileProcessed = 0;
        foreach (var filename in filesFrom(argsRest))
        {
            LogDebug("File:'{0}'", filename);
            (var inpFs, var close, var printFilename) = OpenTextFile(filename);
            int cntLine = 0;
            int cntFound = 0;
            Counter.Reset();
            string? lineRead;
            while (null != (lineRead = inpFs.ReadLine()))
            {
                cntLine += 1;
                LogDebug("{0}:'{1}'", cntLine, lineRead);
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
            LogVerbose("No file is processed.");
        }
        return true;
    }

    static (StreamReader, Action<StreamReader>, Action PrintFilename)
        OpenTextFile(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            return (new StreamReader(
                Console.OpenStandardInput()),
                (_) => { }, () => { });
        }
        return (File.OpenText(filename), (it) => it.Close(),
            () => Console.Write($"{filename}:"));
    }

    //static Action<int> PrintLineNumber { get; set; } = (_) => { }; ** TODO
    static Action<string, int> PrintFoundCount { get; set; } = (_, _)  => { };
    record PostMatchParam(string Path, int Index, string Line,
        MatchCollection Matches, Action PrintFilename);

    static Func<PostMatchParam, bool> DefaultPostMatch { get; set; } = (param) =>
    {
        (var filename, var lineNbr, var lineRead,
        var matches, var printFilename) = param;
        var foundQueue = new List<(int, int)>();
        var lastIndex = 0;
        foreach (Match match in matches)
        {
            foundQueue.Add((match.Index, match.Length));
            lastIndex = match.Index + match.Length;
        }
        printFilename();
        //PrintLineNumber(lineNbr); ** TODO
        var rtn = Counter.Print(lineNbr);
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
        return rtn;
    };
}
