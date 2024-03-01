using System.Text.RegularExpressions;

namespace grep;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            RunMain(args);
        }
        catch (Exception ee)
        {
            Console.WriteLine(ee);
        }
    }

    static bool PrintSyntax()
    {
        Console.WriteLine($"""
            {nameof(grep)} [OPTIONS] REGEX FILE [FILE ..]
            {nameof(grep)} [OPTIONS] REGEX --files-from FILES-FROM
            {nameof(grep)} [OPTIONS] REGEX -T FILES-FROM            
            """);
        return false;
    }

    const Char SpecThe = (Char)27;
    static bool RunMain(string[] args)
    {
        if (args.Length < 2) return PrintSyntax();

        var qry = from arg in args
                  join help in new string[] { "-?", "-h", "--help" }
                  on arg equals help into helpQuery
                  from found in helpQuery select found;
        if (qry.Any()) return PrintSyntax();

        var SpecText = SpecThe.ToString();
        var oldColor = Console.ForegroundColor;
        var redColor = ConsoleColor.Red;

        var regEx = new Regex(args[0]);
        foreach (var filename in args
            .Skip(1)
            .Where((it) => File.Exists(it)))
        {
            using (var inpFs = File.OpenText(filename))
            {
                int cntLne = 0;
                string? lineRead;
                while (null != (lineRead = inpFs.ReadLine()))
                {
                    cntLne += 1;
                    var matches = regEx.Matches(lineRead);
                    if (true != matches.Any()) continue;
                    var foundQueue = new List<(int,int)>();
                    var lastIndex = 0;
                    foreach (Match match in matches)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"line#{cntLne}: {match.Index},{match.Length}");
                        foundQueue.Add((match.Index, match.Length));
                        lastIndex = match.Index + match.Length;
                    }
                    Console.Write($"{filename}:");
                    var ndxLast = 0;
                    foreach ((var ndx, var len) in foundQueue)
                    {
                        switch (ndxLast, ndx)
                        {
                            case (0, 0): break;
                            default:
                                Console.ForegroundColor = oldColor;
                                Console.Write(lineRead.Substring(ndxLast, ndx - ndxLast));
                                break;
                        }
                        Console.ForegroundColor = redColor;
                        Console.Write(lineRead.Substring(ndx, len));
                        ndxLast = ndx + len;
                    }
                    Console.ResetColor();
                    if (ndxLast < lineRead.Length)
                    {
                        Console.Write(lineRead.Substring(ndxLast));
                    }
                    Console.WriteLine();
                }
            }
        }
        return true;
    }
}
