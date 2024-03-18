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

    static public readonly IInvoke<string, bool> ExclFile =
        new ParseInvoker<string, bool>(TextExclFile, help: "FILE-WILD ..",
            init: Always<string>.Never, resolve: (opt, argsThe) =>
            {
                var regexs = argsThe
                .Select((it) => it.Arg)
                .Distinct()
                .Select((it) => it.ToRegexText())
                .Select((it) => ToRegex.Invoke(it))
                .ToArray();
                Func<string, bool> exclMatching = (arg) =>
                regexs
                .Select((it) => it.Match(arg))
                .Where((it) => it.Success).Any();
                opt.SetImplementation((it) => exclMatching(it));
            });

    static public readonly IInvoke<string, bool> ExclDir =
        new ParseInvoker<string, bool>(TextExclDir, help: "DIR-WILD ..",
            init: Always<string>.Never, resolve: (opt, argsThe) =>
            {
                var regexs = argsThe
                .Select((it) => it.Arg)
                .Distinct()
                .Select((it) => it.ToRegexText())
                .Select((it) => ToRegex.Invoke(it))
                .ToArray();
                Func<string, bool> exclMatching = (arg) =>
                regexs
                .Select((it) => it.Match(arg))
                .Where((it) => it.Success).Any();
                opt.SetImplementation((it) => exclMatching(it));
            });

    public record FileScanParam(bool IsPatternFromRedirConsole, string[] Paths);

    static public readonly IInvoke<FileScanParam, IEnumerable<string>>
        FileScan = new ParseInvoker<FileScanParam, IEnumerable<string>>(
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
                string wild2 = "";
                Func<string, string, string> joinPath =
                (path, file) => Path.Join(path, file);

                Log.Debug($"{opt.Name} dir='{dirThe}'");
                if (true != Directory.Exists(dirThe))
                {
                    wild2 = Path.GetFileName(dirThe) ?? "*.*";
                    dirThe = Path.GetDirectoryName(dirThe) ?? ".";
                }
                if (dirThe == ".")
                {
                    joinPath = (_, file) => file;
                }

                Func<string, bool> makeMatching(string[] patterns)
                {
                    Func<string, bool> rtn = Always<string>.True;
                    if (patterns.Length > 0)
                    {
                        var tmp2 = patterns
                        .Select((it) => it.ToRegexText())
                        .Select((it) => ToRegex.Invoke(it))
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
                    var wilds = arg.Paths;
                    if (false == string.IsNullOrEmpty(wild2))
                    {
                        wilds = wilds.Union([wild2]).ToArray();
                    }
                    var nameMatching = makeMatching(wilds);

                    return Dir.Scan
                    .ListFiles(dirThe,
                    filterDirname: (parentThe, dirName) =>
                    {
                        return true != ExclDir.Invoke(dirName);
                    })
                    .Where((it) =>
                    {
                        var filename = Path.GetFileName(it);
                        var rtn2 = nameMatching(filename);
                        var rtn3 = ExclFile.Invoke(filename);
                        return rtn2 && (false == rtn3);
                    })
                    .Select((it) => joinPath(dirThe, it));
                });
            });
}
