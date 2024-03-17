using System.Collections.Immutable;

namespace grep;
using static grep.Options;

internal class Else<R, L>
{
    public bool IsRight { get; init; }
    public Func<R> Right { get; init; }
    public Func<L> Left { get; init; }
    public Else(R right)
    {
        IsRight = true;
        Right = () => right;
        Left = () => throw new NullReferenceException(nameof(Left));
    }
    public Else(L left)
    {
        IsRight = false;
        Left = () => left;
        Right = () => throw new NullReferenceException(nameof(Right));
    }
}

static public partial class MyOptions
{
    internal abstract class Parser : IParse
    {
        public string Name { get; init; }

        public string Help { get; init; }
        public string ExtraHelp { get; init; }
        public Action<Parser, IEnumerable<FlagedArg>> Resolve
        { get; init; }

        public Parser(string name, string help,
            Action<Parser, IEnumerable<FlagedArg>> resolve,
            string extraHelp = "")
        {
            Name = name;
            Help = help;
            ExtraHelp = extraHelp;
            Resolve = resolve;
        }

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

    internal class SimpleParser : Parser
    {
        public SimpleParser(string name,
            Action<Parser, IEnumerable<FlagedArg>> resolve,
            string help = "", string extraHelp = "")
            : base(name, help, resolve, extraHelp)
        {
        }
    }

    internal class ParseInvoker<T, R> : Parser, IInvoke<T, R>
    {
        protected Func<T, R> Imp { get; private set; }
        public ParseInvoker(string name, Func<T, R> @init,
            Action<ParseInvoker<T, R>, IEnumerable<FlagedArg>> resolve,
            string help = "", string extraHelp = "") :
            base(name, help,
                resolve: (obj, args) =>
                resolve((ParseInvoker<T, R>)obj, args),
                extraHelp: extraHelp)
        {
            Imp = @init;
        }

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

    internal class SwitchInvoker<T, R> : Parser, IInvoke<T, R>
    {
        protected Func<T, R> Imp { get; private set; }
        public R Invoke(T arg)
        {
            return Imp(arg);
        }

        public SwitchInvoker(string name, Func<T, R> @init,
            bool alterFor, Func<T, R> alter,
            Action<bool>? alterPost = null,
            string help = "", string? extraHelp = null) :
            base(name, help,
                extraHelp: string.IsNullOrEmpty(extraHelp)
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

                if (flagSwitch.IsRight)
                {
                    if (alterFor == flagSwitch.Right())
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
            })
        {
            Imp = @init;
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
