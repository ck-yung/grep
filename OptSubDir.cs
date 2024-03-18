using System.Text.RegularExpressions;
using static grep.MyOptions;
using static grep.Options;

namespace grep;

static public class SubDir
{
    static string ToRegexText(this string arg)
    {
        var regText = new System.Text.StringBuilder("^");
        regText.Append(arg
            .Replace(@"\", @"\\")
            .Replace("^", @"\^")
            .Replace("$", @"\$")
            .Replace(".", @"\.")
            .Replace("+", @"\+")
            .Replace("?", ".")
            .Replace("*", ".*")
            .Replace("(", @"\(")
            .Replace(")", @"\)")
            .Replace("[", @"\[")
            .Replace("]", @"\]")
            .Replace("{", @"\{")
            .Replace("}", @"\}")
            ).Append('$');
        return regText.ToString();
    }

    public record ScanParam(bool IsPatternFromRedirConsole, string[] Paths);

    static public readonly IInvoke<ScanParam, IEnumerable<string>>
        FileScan = new ParseInvoker<ScanParam, IEnumerable<string>>(
            name: TextSubDir, help: "DIR-NAME",
            extraHelp: $"For example, {nameof(grep)} {TextSubDir} demoDir ..",
            init: (arg) => arg.Paths
            .Select((it) => it.FromWildCard())
            .SelectMany((it) => it)
            .Union(FilesFrom.Invoke(
                new FilesFromParam(
                    arg.IsPatternFromRedirConsole,
                    IsArgsEmpty: arg.Paths.Length == 0)))
            .Distinct(),
            resolve: (opt, argsThe) =>
            {
                var args = argsThe.Distinct().Take(2).ToArray();
                if (args.Length > 1)
                {
                    throw new ConfigException(
                        $"Too many values ({args[0].Arg},{args[1].Arg}) to {opt.Name}");
                }

                var dirThe = args[0].Arg;
                if (true != Directory.Exists(dirThe))
                {
                    throw new ArgumentException($"Dir '{dirThe}' is NOT found!");
                }
                Log.Debug($"{opt.Name} dir='{dirThe}'");

                Func<string, bool> makeMatching(string[] patterns)
                {
                    Func<string, bool> rtn = Always<string>.True;
                    if (patterns.Length > 0)
                    {
                        var tmp2 = patterns
                        .Select((it) =>
                        {
                            Log.Debug($"{opt.Name} pattern:'{it}' before ToRegexText");
                            return it;
                        })
                        .Select((it2) => it2.ToRegexText())
                        .Select((it2) =>
                        {
                            Log.Debug($"{opt.Name} pattern:'{it2}' before regex");
                            return new Regex(it2);
                        })
                        .ToArray();

                        rtn = (it) => tmp2
                        .Select((it2) => it2.Match(it))
                        .Where((it2) => it2.Success)
                        .Any();
                    }
                    else
                    {
                        Log.Debug($"{opt.Name} patterns is EMTPY!");
                    }

                    return rtn;
                }

                opt.SetImplementation((arg) =>
                {
                    var nameMatching = makeMatching(arg.Paths);

                    return Dir.Scan
                    .ListFiles(dirThe,
                    filterDirname: (parentThe, dirName) =>
                    {
                        if (dirName.StartsWith(".git")) return false;
                        //var rtn = nameMatching(fileName);
                        //Show.LogVerbose.Invoke($"Checking '{dirName}':'{fileName}'>{rtn}");
                        return true;
                    })
                    .Where((it) =>
                    {
                        var filename = Path.GetFileName(it);
                        var rtn = nameMatching(filename);
                        return rtn;
                    })
                    .Select((it) => Path.Join(dirThe, it));
                });
            });
}
