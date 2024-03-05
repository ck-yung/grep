using System.Collections.Immutable;

namespace grep;

record FlagedArg(bool Flag, string Arg);

class MissingValueException(string Message) : Exception(Message)
{
}

static class Options
{
    public static IEnumerable<FlagedArg> ToFlagedArgs(
        this IEnumerable<string> args,
        ImmutableDictionary<string, string> mapShortcuts,
        ImmutableDictionary<string, string[]> mapExpandingShortcuts)
    {
        var itrThe = args.GetEnumerator();
        IEnumerable<string> SeparateCombiningShortcut()
        {
            while (itrThe.MoveNext())
            {
                var current = itrThe.Current;
                if (current.StartsWith("--"))
                {
                    yield return current;
                }
                else if (current.StartsWith("-") && current.Length > 2)
                {
                    foreach (var charThe in current.Skip(1))
                    {
                        yield return "-" + charThe;
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }

        foreach (var arg in SeparateCombiningShortcut())
        {
            if (arg.Length == 2 && arg[0] == '-')
            {
                if (mapShortcuts.TryGetValue(arg, out var found))
                {
                    yield return new(false, found);
                }
                else if (mapExpandingShortcuts.TryGetValue(arg, out var expands))
                {
                    foreach (var expand in expands)
                    {
                        yield return new(false, expand);
                    }
                }
                else
                {
                    yield return new(false, arg);
                }
            }
            else
            {
                yield return new(false, arg);
            }
        }
    }

    static IEnumerable<FlagedArg> SelectWithFlag(
        IEnumerable<FlagedArg> args,
        string name, Func<string> whenMissingValue)
    {
        var itrThe = args.GetEnumerator();
        while (itrThe.MoveNext())
        {
            var current = itrThe.Current;
            if (current.Arg == name)
            {
                if (itrThe.MoveNext())
                {
                    yield return new FlagedArg(true, itrThe.Current.Arg);
                }
                else
                {
                    throw new MissingValueException(whenMissingValue());
                }
            }
            else
            {
                yield return current;
            }
        }
    }

    public static (Func<T, R>, IEnumerable<FlagedArg>) Parse<T, R>(
        IEnumerable<FlagedArg> args, string name,
        Func<T, R> init, Func<string, Func<T, R>> parse,
        Func<string>? whenMissingValue = null)
    {
        string DefaultMissingValue() => $"Missing value to {name}";

        var qryFlaged = SelectWithFlag(args, name,
            whenMissingValue ?? DefaultMissingValue)
            .GroupBy((it) => it.Flag)
            .ToDictionary((grp) => grp.Key,
            elementSelector: (grp) => grp.AsEnumerable());

        IEnumerable<FlagedArg> qryNotFound = [];
        if (qryFlaged.TryGetValue(false, out var tmpNotFound))
        {
            qryNotFound = tmpNotFound;
        }

        if (qryFlaged.TryGetValue(true, out var qryFound))
        {
            var foundThe = qryFound.Distinct().ToArray();
            if (foundThe.Length > 1)
            {
                throw new ArgumentException(
                    $"Too many value ('{foundThe[0].Arg}','{foundThe[1].Arg}') to {name}");
            }
            return (parse(foundThe[0].Arg), qryNotFound);
        }
        return (init, qryNotFound);
    }

    public static (Func<T, R>, IEnumerable<FlagedArg>) ParseStrings<T, R>(
        IEnumerable<FlagedArg> args, string name,
        Func<T, R> init, Func<IEnumerable<string>, Func<T, R>> parse,
        Func<string>? whenMissingValue = null)
    {
        string DefaultMissingValue() => $"Missing value to {name}";

        var qryFlaged = SelectWithFlag(args, name,
            whenMissingValue ?? DefaultMissingValue)
            .GroupBy((it) => it.Flag)
            .ToDictionary((grp) => grp.Key,
            elementSelector: (grp) => grp.AsEnumerable());

        IEnumerable<FlagedArg> qryNotFound = [];
        if (qryFlaged.TryGetValue(false, out var tmpNotFound))
        {
            qryNotFound = tmpNotFound;
        }

        if (qryFlaged.TryGetValue(true, out var qryFound))
        {
            var aa = qryFound.Select((it) => it.Arg);
            return (parse(aa), qryNotFound);
        }
        return (init, qryNotFound);
    }
}
