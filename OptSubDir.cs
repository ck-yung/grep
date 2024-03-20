using System.Collections.Immutable;
using static grep.MyOptions;
using static grep.Options;
using static Dir.Wild;
using System.Text.RegularExpressions;

namespace grep;

static public class SubDir
{
    static public readonly IInvoke<string, bool> ExclFile =
        new ParseInvoker<string, bool>(TextExclFile, help: "FILE",
            init: Always<string>.Never, resolve: (opt, argsThe) =>
            {
                var exclMatchings = argsThe
                .Select((it) => it.Arg)
                .Distinct()
                .Select((it) => Dir.Wild.ToWildMatch(it))
                .ToArray();

                if (exclMatchings.Length > 0)
                {
                    opt.SetImplementation((text) =>
                    exclMatchings.Any((it) => it(text)));
                }
            });

    static Func<string, bool> ExclDirPostifx = Always<string>.Never;

    static public readonly IInvoke<string, bool> ExclDir =
        new ParseInvoker<string, bool>(TextExclDir, help: "DIR",
            init: Always<string>.Never, resolve: (opt, argsThe) =>
            {
                var exclMatchings = argsThe
                .Select((it) => it.Arg)
                .Distinct()
                .Select((it) => Dir.Wild.ToWildMatch(it))
                .ToArray();

                if (exclMatchings.Length > 0)
                {
                    opt.SetImplementation((text) =>
                    exclMatchings.Any((match) => match(text)));
                }

                Func<string, bool> MakeMatchRegex(Regex regex)
                => (arg) => regex.Match(arg).Success;

                var exclDirPostfixMatchings = argsThe
                .Select((it) => it.Arg)
                .Select((it) => it.ToRegexText())
                .Select((it) => it.TrimStart('^').TrimEnd('$'))
                .Distinct()
                .Select((it) => it.MakeRegex())
                .Select((it) => MakeMatchRegex(it))
                .ToArray();
                if (exclDirPostfixMatchings.Length > 0)
                {
                    ExclDirPostifx = (arg) =>
                    exclDirPostfixMatchings.Any((match) => match(arg));
                }
            });

    record MatchingDirParam(string DirName,
        Func<string,bool> Matching, Func<string, string, string> JoinFunc);

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
                            Directory = Path.GetDirectoryName(it) ?? "",
                            Filename = Path.GetFileName(it)
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
                    .Select((it) => Dir.Wild.ToWildMatch(it))
                    .ToArray();

                    return (it) => regexes
                    .Any((match) => match(it));
                }

                var matchingFunc = makeMatching(wilds2);

                Func<string, string, string> makeJoinFunc(string dirThe)
                {
                    if (dirThe == ".") return (_, file) => file;
                    return (dir2, file2) => Path.Join(dirThe, file2);
                }

                IEnumerable<string> makeFunc2(
                    IEnumerable<MatchingDirParam> args) => args
                    .Select((it) => Dir.Scan.ListFiles(it.DirName,
                    filterDirname: (parentThe, dirName) =>
                    false == ExclDir.Invoke(dirName))
                    .Where((it2) =>
                    {
                        var filename = Path.GetFileName(it2);
                        return it.Matching(filename) &&
                        (false == ExclFile.Invoke(filename));
                    })
                    .Select((it2) => it.JoinFunc(it.DirName, it2)))
                    .SelectMany((it2) => it2)
                    .Where((it) => false == ExclDirPostifx(
                        Path.GetDirectoryName(it) ?? string.Empty));


                var rtn2 = makeFunc2(dirs
                .Select((dir) => new MatchingDirParam(
                    DirName: dir,
                    Matching: matchingFunc,
                    JoinFunc: makeJoinFunc(dir)
                )));

                var rtn3 = makeFunc2(map2nd
                .Select((it) => new MatchingDirParam(
                    DirName: it.Key,
                    Matching: makeMatching(it.Value.Union(wilds2)),
                    JoinFunc: makeJoinFunc(it.Key)
                )));

                return rtn2.Union(rtn3);
            });
}