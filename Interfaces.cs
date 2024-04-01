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
    public record Info(ArgType Type, IParse? Option,
        Else<Exception, string> Error);

    static readonly List<Info> Errors = [];

    static public void Add(ArgType type, Exception e, IParse? option = null)
    {
        Errors.Add(new Info(type, option, new Else<Exception, string>(e)));
    }

    static public void Add(ArgType type, string message, IParse? option = null)
    {
        Errors.Add(new Info(type, option, new Else<Exception, string>(message)));
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

    static internal void WrongValue(FlagedArg arg, IParse opt)
    {
        Add(arg.Type, $"Value '{arg.Arg}' to {opt.Name} is NOT valid!", opt);
    }

    static internal void TooManyValues(FlagedArg arg1, FlagedArg arg2, IParse opt)
    {
        Add(arg1.Type, $"Too many values ('{arg1.Arg}','{arg2.Arg}') to {opt.Name}", opt);
    }

    public static int PrintErrors() => Errors
        .Select((errorThe) =>
        {
            var envrThe = errorThe.Type == ArgType.Environment
            ? $"Envir '{nameof(grep)}': " : string.Empty;
            var optThe = errorThe.Option;
            if (errorThe.Error.IsFirst)
            {
                Exception exceptionThe = errorThe.Error.First();
                var errType = exceptionThe.GetType().ToString()
                    .Replace("grep.ConfigException", "")
                    .Replace("System.", "")
                    .Replace("Exception", "");
                if (string.IsNullOrEmpty(errType))
                {
                    Show.LogVerbose.Invoke(
                        $"{envrThe}{exceptionThe.Message}");
                }
                else
                {
                    Show.LogVerbose.Invoke(
                        $"{envrThe}({errType}) {exceptionThe.Message}");
                }
            }
            else
            {
                Show.LogVerbose.Invoke(
                    $"{envrThe}{errorThe.Error.Second()}");
            }

            if (false == string.IsNullOrEmpty(optThe?.Help))
            {
                Show.LogVerbose.Invoke(
                    $"Valid value is one of {optThe.Help}");
            }

            if (false == string.IsNullOrEmpty(optThe?.ExtraHelp))
            {
                Show.LogVerbose.Invoke(optThe.ExtraHelp);
            }
            return errorThe;
        })
        .Count();
}

internal class NoMessageException : Exception;
