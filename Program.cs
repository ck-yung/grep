using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;

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

        Log.Verbose("Do nothing.");
        return true;
    }
}
