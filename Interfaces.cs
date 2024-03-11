using System.Text;

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

public interface IInovke<T, R>
{
    R Invoke(T arg);
}

public class ImplicitBool
{
    protected bool Flag { get; set; } = false;
    static public implicit operator bool(ImplicitBool the)
    {
        return the.Flag;
    }
}

internal partial class Helper
{
    public const string ExtraHelp = "+?";

    public static string GetUnique(IEnumerable<string> args, IParse opt)
    {
        var rtn = args
            .Where((it) => it.Length > 0)
            .Distinct(comparer: StringComparer.InvariantCultureIgnoreCase)
            .Take(2)
            .ToArray();

        if (rtn.Length == 0)
        {
            throw ConfigException.MissingValue(opt.Name);
        }

        if (rtn.Length > 1)
        {
            throw new ConfigException($"Too many values ({rtn[0]};{rtn[1]}) to '{opt.Name}'");
        }

        return rtn[0];
    }
}

//public class ShowSyntaxException : Exception
//{
//    internal ShowSyntaxException(IParse parser)
//        : base(MyOptions.GetHelpText(parser))
//    {
//        if (false ==
//            string.IsNullOrEmpty(parser.ExtraHelp))
//        {
//            base.Data["extra"] = parser.ExtraHelp;
//        }
//    }
//}

internal class ConfigException : Exception
{
    public record Info(ArgType Type, string Source, Exception Error);

    public ConfigException(string message) : base(message)
    {
    }

    static readonly IList<Info> Errors = new List<Info>();

    static public void Add(ArgType type, string source, Exception e)
    {
        Errors.Add(new Info(type, source, e));
    }

    static public IEnumerable<Info> GetErrors()
    {
        return Errors;
    }

    static internal ConfigException MissingValue(string name)
    {
        var msg = new StringBuilder();// Shortcut:
        msg.AppendLine($"Missing value to '{name}'");
        //var shortcutFound = MyOptions.GetShortCut(name);
        //if (string.IsNullOrEmpty(shortcutFound))
        //{
        //    shortcutFound = name;
        //}
        //msg.Append($"Try '{nameof(grep)} {shortcutFound} +?'");
        return new(msg.ToString());
    }
}
