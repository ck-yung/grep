using System.Collections.Immutable;

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

    const string OptColor = "--color"; // ---------------------- TODO
    const string OptFilesFrom = "--files-from"; // ------------- TODO
    const string OptCaseSensitive = "--case-sensitive"; // ----- TODO
    const string OptWord = "--word"; // ------------------------ TODO
    const string OptLineNumber = "--line-number"; // ----------- TODO
    const string OptCountOnly = "--count"; // ------------------ TODO
    const string OptFileMatch = "--file-match"; // ------------- TODO
    const string OptInvertMatch = "--invert-match"; // --------- TODO
    const string OptRegexFile = "--regex-file"; // ------------- TODO
    const string OptQuiet = "--quiet"; // ---------------------- TODO
    const string OptMaxCount = "--max-count"; // --------------- TODO
    const string OptShowFilename = "--show-filename"; // ------- TODO
    const string OptFixedTextFrom = "--fixed-text-file"; // ---- TODO
    const string OptPause = "--pause"; // ---------------------- TODO

    static bool RunMain(string[] args)
    {
        if (args.Any((it) => it == "--version"))
            return PrintVersion();

        var qry = from arg in args
                  join help in new string[] { "--help", "-?" }
                  on arg equals help into helpFound
                  from found in helpFound select found;
        if (qry.Any()) return PrintSyntax(isDetailed: true);

        var flagedArgs = args.ToFlagedArgs(new Dictionary<string, string[]>()
        {
            ["-f"] = [OptRegexFile],
            ["-F"] = [OptFixedTextFrom],
            ["-m"] = [OptMaxCount],
            ["-T"] = [OptFilesFrom],

            ["-c"] = [OptCountOnly, TextOn],
            ["-h"] = [OptShowFilename, TextOff],
            ["-i"] = [OptCaseSensitive, TextOff],
            ["-l"] = [OptFileMatch, TextOn],
            ["-n"] = [OptLineNumber, TextOn],
            ["-p"] = [OptPause, TextOff],
            ["-q"] = [OptQuiet, TextOn],
            ["-v"] = [OptInvertMatch, TextOn],
            ["-w"] = [OptWord, TextOn],
        }.ToImmutableDictionary());

        (var patternText, var paths) = GetRegexPaths(args);

        if (string.IsNullOrEmpty(patternText))
        {
            PrintSyntax();
            return false;
        }

        var patternThe = new Pattern(patternText);
        if (paths.Length == 0)
        {
            if (true != Console.IsInputRedirected)
            {
                PrintSyntax();
                return false;
            }

            var cnt = ReadAllLinesFromConsole()
                .Select((line) => new { line, matches = patternThe.Matches(line) })
                .Where((it) => it.matches.Length > 0)
                .Select((it, index) =>
                {
                    Console.Write($"{index+1}:");
                    Console.WriteLine(it.line);
                    return it;
                })
                .Count();
            Log.Verbose($":{cnt}");
        }
        else
        {
            foreach (var path in paths
                .Select((it) => it.FromWildCard())
                .SelectMany((it) => it))
            {
                var cnt = ReadAllLinesFromFile(path)
                    .Select((line) => new { line, matches = patternThe.Matches(line) })
                    .Where((it) => it.matches.Length > 0)
                    .Select((it, index) =>
                    {
                        Console.Write($"{index+1}:");
                        Console.WriteLine(it.line);
                        return it;
                    })
                    .Count();
                if (cnt>0) Log.Verbose($"{path}:{cnt}");
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
