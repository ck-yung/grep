using System.Collections.Immutable;
using static grep.MyOptions;
using static grep.Options;
using static Dir.Wild;

namespace grep;

static public class SubDir
{
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

                bool exclMatching(string arg) =>
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

                bool exclMatching(string arg) =>
                regexs
                .Select((it) => it.Match(arg))
                .Where((it) => it.Success).Any();

                opt.SetImplementation((it) => exclMatching(it));
            });

    static public readonly IInvoke<string[], IEnumerable<string>>
        FileScan = new SwitchInvoker<string[], IEnumerable<string>>(
            TextSubDir, alterFor: true,
            init: (paths) => paths
            .Select((it) => it.FromWildCard())
            .SelectMany((it) => it),
            alter: (wilds) =>
            {
                var map1st = wilds
                .GroupBy((it) => Directory.Exists(it))
                .ToImmutableDictionary((grp) => grp.Key,
                (grp) => grp.Select((it) => it));

                IEnumerable<string> dirs = [];
                if (map1st.TryGetValue(true, out var temp5))
                {
                    dirs = temp5.AsEnumerable();
                }

                IEnumerable<string> wilds2 = [];

                var map2nd = ImmutableDictionary<string, string[]>.Empty;

                if (map1st.TryGetValue(false, out var temp2))
                {
                    var aa2 = temp2
                        .Select((it) => new
                        {
                            Directory=Path.GetDirectoryName(it) ?? "",
                            Filename=Path.GetFileName(it)
                        })
                        .GroupBy((it) => string.IsNullOrEmpty(it.Directory))
                        .ToImmutableDictionary((grp) => grp.Key,
                        (grp) => grp);
                        if (aa2.TryGetValue(true, out var temp3))
                        {
                            wilds2 = temp3
                            .Select((it) => it.Filename);
                        }
                        if (aa2.TryGetValue(false, out var temp4))
                        {
                            map2nd = temp4
                            .GroupBy((it) => it.Directory)
                            .ToImmutableDictionary((grp) => grp.Key,
                            (grp) => grp.Select((it) => it.Filename).ToArray());
                        }
                }

                if (false == dirs.Any() && false == map2nd.Any())
                {
                    dirs = ["."];
                }

                Func<string, bool> makeMatching(IEnumerable<string> patterns)
                {
                    if (false == patterns.Any())
                    {
                        Log.Debug($"{nameof(makeMatching)} patterns is EMTPY");
                        return Always<string>.True;
                    }

                    var regexes = patterns
                    .Distinct()
                    .Select((it) => it.ToRegexText())
                    .Select((it) => ToRegex.Invoke(it))
                    .ToArray();

                    return (it) => regexes
                    .Select((regex) => regex.Match(it))
                    .Any((match) => match.Success);
                }

                var matchingFunc = makeMatching(wilds2);

                Func<string, string, string> makeJoinFunc(string dirThe)
                {
                    if (dirThe == ".") return (_, file) => file;
                    return (dir2, file2) => Path.Join(dirThe, file2);
                }

                var rtn2 = dirs
                .Select((dir) => new
                {
                    DirName = dir,
                    JoinFunc = makeJoinFunc(dir)
                })
                .Select((it) => Dir.Scan.ListFiles(it.DirName,
                    filterDirname: (parentThe, dirName) =>
                    {
                        return true != ExclDir.Invoke(dirName);
                    })
                    .Where((it2) =>
                    {
                        var filename = Path.GetFileName(it2);
                        return matchingFunc(filename) &&
                        (false == ExclFile.Invoke(filename));
                    })
                    .Select((it2) => it.JoinFunc(it.DirName, it2)))
                .SelectMany((it2) => it2);

                var rtn3 = map2nd
                .Select((it) => new
                {
                    DirName = it.Key,
                    Matching = makeMatching(it.Value.Union(wilds2)),
                    JoinFunc = makeJoinFunc(it.Key)
                })
                .Select((it) => Dir.Scan.ListFiles(it.DirName,
                    filterDirname: (parentThe, dirName) =>
                    {
                        return true != ExclDir.Invoke(dirName);
                    })
                    .Where((it2) =>
                    {
                        var filename = Path.GetFileName(it2);
                        return it.Matching(filename) &&
                        (false == ExclFile.Invoke(filename));
                    })
                    .Select((it2) => it.JoinFunc(it.DirName, it2)))
                .SelectMany((it2) => it2);

                return rtn2.Union(rtn3);
            });
}
