namespace grep;

public enum ArgType
{
    CommandLine,
    Environment,
};

static class Config
{
    static IEnumerable<(ArgType, string)> SelectArgsFromLines(
        ArgType type, IEnumerable<string> args)
    {
        foreach (var arg in args
            .Select((it) => it.Trim())
            .Where((it) => it.StartsWith("--")))
        {
            var bb = arg.Split([' ', '\t'], 2);
            if (bb.Length == 2)
            {
                yield return (type, $"{bb[0].Trim()}");
                var b2 = bb[1].Trim();
                if (false == string.IsNullOrEmpty(b2))
                {
                    yield return (type, $"{b2}");
                }
            }
            else if (bb.Length == 1)
            {
                yield return (type, $"{bb[0].Trim()}");
            }
        }
    }

    static internal IEnumerable<(ArgType, string)> GetEnvirOpts()
    {
        var envirOld = Environment.GetEnvironmentVariable(nameof(grep));
        if (string.IsNullOrEmpty(envirOld)) return [];

        try
        {
            var aa = envirOld.Split("--")
                .Select((it) => it.Trim())
                .Select((it) => it.Trim(';'))
                .Where((it) => it.Length > 0)
                .Select((it) => "--" + it);

            return SelectArgsFromLines(
                ArgType.Environment, aa);
        }
        catch (Exception ee)
        {
            ConfigException.Add(ArgType.Environment, nameof(grep), ee);
            return [];
        }
    }
}
