using System.Collections.Immutable;

namespace grep;
using static grep.Options;

internal class Else<F, S>
{
    public bool IsFirst { get; init; }
    public Func<F> First { get; init; }
    public Func<S> Second { get; init; }
    public Else(F right)
    {
        IsFirst = true;
        First = () => right;
        Second = () => throw new
        NullReferenceException(nameof(Second));
    }
    public Else(S left)
    {
        IsFirst = false;
        Second = () => left;
        First = () => throw new
        NullReferenceException(nameof(First));
    }
}

static public partial class MyOptions
{
    internal abstract class Parser(string name, string help,
        Action<MyOptions.Parser, IEnumerable<FlagedArg>> resolve,
        string extraHelp = "") : IParse
    {
        public string Name { get; init; } = name;
        public string Help { get; init; } = help;
        public string ExtraHelp { get; init; } = extraHelp;
        public Action<Parser, IEnumerable<FlagedArg>> Resolve
        { get; init; } = resolve;

        public IEnumerable<FlagedArg> Parse(
            IEnumerable<FlagedArg> args)
        {
            IEnumerable<FlagedArg> ToFlagEnum()
            {
                var it = args.GetEnumerator();
                while (it.MoveNext())
                {
                    var current = it.Current;
                    if (current.Arg != Name)
                    {
                        yield return it.Current;
                    }
                    else
                    {
                        if (!it.MoveNext())
                        {
                            throw ConfigException.MissingValue(Name, ExtraHelp);
                        }
                        yield return
                            new(true, it.Current.Type, it.Current.Arg);
                    }
                }
            }

            var groupThe = ToFlagEnum()
                .GroupBy((it) => it.Flag)
                .ToImmutableDictionary((it) => it.Key, (it) => it.AsEnumerable());

            if (groupThe.TryGetValue(true, out var matches))
            {
                Resolve(this, matches);
            }

            if (groupThe.TryGetValue(false, out var notMatches))
            {
                return notMatches;
            }

            return [];
        }
    }

    internal class SimpleParser(string name,
        Action<MyOptions.Parser, IEnumerable<FlagedArg>> resolve,
        string help = "", string extraHelp = "")
        : Parser(name, help, resolve, extraHelp)
    {
    }

    internal class ParseInvoker<T, R>(string name, Func<T, R> @init,
        Action<MyOptions.ParseInvoker<T, R>, IEnumerable<FlagedArg>> resolve,
        string help = "", string extraHelp = "") : Parser(name, help,
            resolve: (obj, args) =>
                resolve((ParseInvoker<T, R>)obj, args),
            extraHelp: extraHelp), IInvoke<T, R>
    {
        protected Func<T, R> Imp { get; private set; } = @init;

        public R Invoke(T arg)
        {
            return Imp(arg);
        }

        public bool SetImplementation(Func<T, R> impNew)
        {
            if (impNew != null)
            {
                Imp = impNew;
                return true;
            }
            return false;
        }
    }

    internal class SwitchInvoker<T, R>(string name, Func<T, R> @init,
        bool alterFor, Func<T, R> alter,
        Action<bool>? alterPost = null,
        string help = "", string? extraHelp = null) :
        Parser(name, help,
            extraHelp : string.IsNullOrEmpty(extraHelp)
            ? $"Value to {name} is on or off." : extraHelp,

            resolve: (opt, args) =>
            {
                var argsThe = args.Distinct().Take(2).ToArray();
                if (argsThe.Length > 1)
                {
                    ConfigException.Add(argsThe[0].Type, new ArgumentException(
                        $"Too many values ('{argsThe[0].Arg}','{argsThe[1].Arg}') are found to '{name}'!"));
                    return;
                }

                var argThe = argsThe[0];
                Else<bool, Ignore> flagSwitch;
                if (TextOn.Equals(argThe.Arg, StringComparison.InvariantCultureIgnoreCase))
                {
                    flagSwitch = new Else<bool, Ignore>(true);
                }
                else if (TextOff.Equals(argThe.Arg, StringComparison.InvariantCultureIgnoreCase))
                {
                    flagSwitch = new Else<bool, Ignore>(false);
                }
                else
                {
                    flagSwitch = new Else<bool, Ignore>(Ignore.Void);
                    ConfigException.Add(argThe.Type, new ArgumentException(
                        $"Value '{argThe.Arg}' to '{name}' is NOT '{TextOn}' or '{TextOff}'!"));
                }

                if (flagSwitch.IsFirst)
                {
                    if (alterFor == flagSwitch.First())
                    {
                        ((SwitchInvoker<T, R>)opt).SetImplementation(alter);
                        alterPost?.Invoke(alterFor);
                    }
                    else
                    {
                        ((SwitchInvoker<T, R>)opt).SetImplementation(init);
                        alterPost?.Invoke(!alterFor);
                    }
                }
            }), IInvoke<T, R>
    {
        protected Func<T, R> Imp { get; private set; } = @init;
        public R Invoke(T arg)
        {
            return Imp(arg);
        }

        public bool SetImplementation(Func<T, R> impNew)
        {
            if (impNew != null)
            {
                Imp = impNew;
                return true;
            }
            return false;
        }
    }
}
