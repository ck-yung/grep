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

    // TODO: Check if needed
    /// <summary>
    /// Implicit boolean, default false
    /// </summary>
    internal class SwitchParser : ImplicitBool, IParse
    {
        public string Name { get; init; }

        public string Help { get; init; }
        public string ExtraHelp { get; init; }
        public Action Action { get; init; }

        public SwitchParser(string name,
            string help = "", string extraHelp = "")
        {
            Name = name;
            Help = help;
            ExtraHelp = extraHelp;
            Action = () => { };
        }

        public SwitchParser(string name, Action action,
            string help = "", string extraHelp = "")
        {
            Name = name;
            Help = help;
            ExtraHelp = extraHelp;
            Action = action;
        }

        public IEnumerable<FlagedArg> Parse(
            IEnumerable<FlagedArg> args)
        {
            bool notYetActed = true;
            var it = args.GetEnumerator();
            while (it.MoveNext())
            {
                var current = it.Current;
                if (current.Arg == Name)
                {
                    Flag = true;
                    if (notYetActed)
                    {
                        notYetActed = false;
                        Action();
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }
    }

    internal abstract class Parser : IParse
    {
        public string Name { get; init; }

        public string Help { get; init; }
        public string ExtraHelp { get; init; }
        public Action<Parser, IEnumerable<string>>
            Resolve
        { get; init; }

        public Parser(string name, string help,
            Action<Parser, IEnumerable<string>> resolve,
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
                            throw ConfigException.MissingValue(Name); // TODO
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
                Resolve(this, matches.Select((it) => it.Arg));
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
            Action<Parser, IEnumerable<string>> resolve,
            string help = "", string extraHelp = "")
            : base(name, help, resolve, extraHelp)
        {
        }
    }

    internal class ParseInvoker<T, R> : Parser, IInvoke<T, R>
    {
        protected Func<T, R> imp { get; private set; }
        public ParseInvoker(string name, Func<T, R> @init,
            Action<ParseInvoker<T, R>, IEnumerable<string>> resolve,
            string help = "", string extraHelp = "") :
            base(name, help,
                resolve: (obj, args) =>
                resolve((ParseInvoker<T, R>)obj, args),
                extraHelp: extraHelp)
        {
            imp = @init;
        }

        public R Invoke(T arg)
        {
            return imp(arg);
        }

        public bool SetImplementation(Func<T, R> impNew)
        {
            if (impNew != null)
            {
                imp = impNew;
                return true;
            }
            return false;
        }
    }

    internal class SwitchInvoker<T, R> : Parser, IInvoke<T, R>
    {
        protected Func<T, R> imp { get; private set; }
        public R Invoke(T arg)
        {
            return imp(arg);
        }

        public SwitchInvoker(string name, Func<T, R> @init,
            bool alterFor, Func<T, R> alter, 
            string help = "", string extraHelp = "") :
            base(name, help, extraHelp: extraHelp, resolve: (opt, args) =>
            {
                var argsThe = args.Distinct().Take(2).ToArray();
                if (argsThe.Length > 1)
                    throw new ArgumentException(
                        $"Too many values ('{argsThe[0]}','{argsThe[1]}') are found to '{name}'!");

                bool isEnabledFlag;
                if (TextOn.Equals(argsThe[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    isEnabledFlag = true;
                }
                else if (TextOff.Equals(argsThe[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    isEnabledFlag = false;
                }
                else
                {
                    throw new ArgumentException(
                        $"Value '{argsThe[0]}' to '{name}' is NOT '{TextOn}' or '{TextOff}'!");
                }

                if (alterFor == isEnabledFlag)
                {
                    ((SwitchInvoker<T, R>)opt).SetImplementation(alter);
                }
                else
                {
                    ((SwitchInvoker<T, R>)opt).SetImplementation(init);
                }
            })
        {
            imp = @init;
        }

        public SwitchInvoker(string name, Func<T, R> @init,
            bool alterFor, Action<bool> alterWhen, Func<T, R> alter,
            string help = "", string extraHelp = "") :
            base(name, help, extraHelp: extraHelp, resolve: (opt, args) =>
            {
                var argsThe = args.Distinct().Take(2).ToArray();
                if (argsThe.Length > 1)
                    throw new ArgumentException(
                        $"Too many values ('{argsThe[0]}','{argsThe[1]}') are found to '{name}'!");

                switch (alterFor,
                TextOn.Equals(argsThe[0], StringComparison.InvariantCultureIgnoreCase),
                TextOff.Equals(argsThe[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    case (true, true, false):
                        alterWhen(true);
                        ((SwitchInvoker<T, R>)opt).SetImplementation(alter);
                        break;
                    case (false, false, true):
                        alterWhen(false);
                        ((SwitchInvoker<T, R>)opt).SetImplementation(alter);
                        break;
                    case (_, false, false):
                        throw new ArgumentException(
                            $"Value '{argsThe[0]}' to '{name}' is NOT '{TextOn}' or '{TextOff}'!");
                    default:
                        break;
                }
            })
        {
            imp = @init;
        }

        public bool SetImplementation(Func<T, R> impNew)
        {
            if (impNew != null)
            {
                imp = impNew;
                return true;
            }
            return false;
        }
    }
}
