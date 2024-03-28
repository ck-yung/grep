namespace grep;

public enum ArgType
{
    CommandLine,
    Environment,
    Never,
}

public record FlagedArg(bool Flag, ArgType Type, string Arg)
{
    static public readonly FlagedArg Never = new(true, ArgType.Never, "Never");
}

public interface IParse
{
    string Name { get; }
    string Help { get; }
    string ExtraHelp { get; }
    public IEnumerable<FlagedArg> Parse(
        IEnumerable<FlagedArg> args);
}

public class NullParser(string name,
    string? help = "", string? extraHelp = "") : IParse
{
    public string Name => name;
    public string Help => help ?? "";
    public string ExtraHelp => extraHelp ?? "";
    public IEnumerable<FlagedArg> Parse(IEnumerable<FlagedArg> _) => [];
}

public interface IInvoke<T, R>
{
    R Invoke(T arg);
}

internal class ConfigException(string message) : Exception(message)
{
    public record Info(ArgType Type, IParse? Option, Exception Error);

    static readonly List<Info> Errors = [];

    static public void Add(ArgType type, Exception e, IParse? option = null)
    {
        Errors.Add(new Info(type, option, e));
    }

    static public IEnumerable<Info> GetErrors()
    {
        return Errors;
    }

    static internal ConfigException MissingValue(
        string name, string extraHelp = "")
    {
        if (string.IsNullOrEmpty(extraHelp))
        {
            return new($"Missing value to '{name}'\n" +
                $"Syntax: {nameof(grep)} -?");
        }
        return new($"Missing value to '{name}'\n" +
            extraHelp);
    }

    static internal void WrongValue(ArgType type, IParse opt,
        string message)
    {
        Errors.Add(new Info(type, opt, new ConfigException(message)));
    }
}

internal class NoMessageException : Exception;
