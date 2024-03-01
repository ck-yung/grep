using System.Collections.Immutable;
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
        Console.WriteLine(isDetailed ? $"{nameof(grep)} -?" : $"{nameof(grep)} --help");
        Console.WriteLine($"""
            {nameof(grep)} [OPTIONS] REGEX FILE [FILE ..]
            {nameof(grep)} [OPTIONS] REGEX --files-from FILES-FROM
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
            
            Short-Cut
              -T       --files-from
              -i       --case-sensitive off
              -w       --word on
              -n       --line-number on
              -c       --count on
              -l       --file-match on
              -v       --invert-match on

            Read console input if FILES-FROM is -
            """);
        }
        return false;
    }

    const string OptFilesFrom = "--files-from";
    const string OptCaseSensitive = "--case-sensitive";
    const string OptWord = "--word";
    const string OptLineNumber = "--line-number";
    const string OptCountOnly = "--count";
    const string OptFileMatch = "--file-match";
    const string OptInvertMatch = "--invert-match";

    static bool RunMain(string[] args)
    {
        if (args.Any((it) => it == "--help")) return PrintSyntax(true);
        if (args.Length < 2) return PrintSyntax();

        var qry = from arg in args
                  join help in new string[] { "-?", "-h"}
                  on arg equals help into helpQuery
                  from found in helpQuery select found;
        if (qry.Any()) return PrintSyntax();

        var flagedArgs = args.ToFlagedArgs(new Dictionary<string, string>()
        {
            ["-T"] = OptFilesFrom,
        }.ToImmutableDictionary(), new Dictionary<string, string[]>()
        {
            ["-i"] = [OptCaseSensitive, "off"],
            ["-w"] = [OptWord, "on"],
            ["-n"] = [OptLineNumber, "on"],
            ["-c"] = [OptCountOnly, "on"],
            ["-l"] = [OptFileMatch, "on"],
            ["-v"] = [OptInvertMatch, "on"],
        }.ToImmutableDictionary());

        (var filesFrom, flagedArgs) = Parse<bool, IEnumerable<string>>(flagedArgs,
            name: OptFilesFrom, init: (_) => Array.Empty<string>(),
            parse: (path) =>
            {
                string? lineThe;
                if ("-" == path)
                {
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

        var argsRest = flagedArgs.Select((it) => it.Arg).ToArray();
        if (argsRest.Length < 1)
        {
            return PrintSyntax();
        }

        var regEx = makeRegex(toWord(argsRest.First()));

        foreach (var filename in filesFrom(true)
            .Union(
            argsRest
            .Skip(1)
            .Select((it) => it.Trim())
            .Distinct())
            .Where((it) => File.Exists(it)))
        {
            using var inpFs = File.OpenText(filename);
            int cntLine = 0;
            int cntFound = 0;
            string? lineRead;
            while (null != (lineRead = inpFs.ReadLine()))
            {
                cntLine += 1;
                var matches = regEx.Matches(lineRead);
                if (true != checkMatches(matches)) continue;
                cntFound += 1;
                if (true != postMatch(new(filename, cntLine, lineRead, matches)))
                {
                    break;
                }
            }
            PrintFoundCount(filename, cntFound);
        }
        return true;
    }

    static ConsoleColor OldColor { get; set; } = Console.ForegroundColor;
    static ConsoleColor RedColor { get; set; } = ConsoleColor.Red;

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
            switch (ndxLast, ndx)
            {
                case (0, 0): break;
                default:
                    Console.ForegroundColor = OldColor;
                    Console.Write(lineRead[ndxLast..ndx]);
                    break;
            }
            Console.ForegroundColor = RedColor;
            Console.Write(lineRead[ndx..(ndx + len)]);
            ndxLast = ndx + len;
        }
        Console.ResetColor();
        if (ndxLast < lineRead.Length)
        {
            Console.Write(lineRead[ndxLast..]);
        }
        Console.WriteLine();
        return true;
    };
}
