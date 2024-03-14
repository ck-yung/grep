namespace grep;

public enum ArgType
{
    CommandLine,
    Environment,
};

public record FlagedArg(bool Flag, ArgType Type, string Arg);


public interface IParse
{
    string Name { get; }
    string Help { get; }
    string ExtraHelp { get; }
    public IEnumerable<FlagedArg> Parse(
        IEnumerable<FlagedArg> args);
}

public interface IInvoke<T, R>
{
    R Invoke(T arg);
}

internal class ConfigException : Exception
{
    public record Info(ArgType Type, string Source, Exception Error);

    public ConfigException(string message) : base(message)
    {
    }

    static readonly IList<Info> Errors = new List<Info>();

    static public void Add(ArgType type, Exception e, string source = "")
    {
        Errors.Add(new Info(type, source, e));
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
}
