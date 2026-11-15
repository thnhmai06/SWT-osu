using System.Text.RegularExpressions;
using Microsoft.Build.Locator;

namespace ConditionCoverage;

public static class Program
{
    private const string usage = """
        instrument --project <csproj> --map <map.json> [--include <regex>] [--exclude <regex>]
            Ghi đè .cs của project bằng bản có probe (chạy trên worktree!). regex khớp đường dẫn tương đối so với project.
        report --maps <a.json,b.json,...> --hits <dir> --out <dir> [--title <text>] [--scope <regex>]
            Gom số liệu hits-*.txt thành báo cáo Markdown (solution > project > namespace > type > method).
        """;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0) { Console.Error.WriteLine(usage); return 2; }

        var opt = new Dictionary<string, string>();
        for (int i = 1; i + 1 < args.Length; i += 2) opt[args[i].TrimStart('-')] = args[i + 1];

        switch (args[0])
        {
            case "instrument":
                MSBuildLocator.RegisterDefaults();
                await Instrumenter.RunAsync(opt["project"], opt["map"],
                    opt.TryGetValue("include", out var inc) ? new Regex(inc) : null,
                    opt.TryGetValue("exclude", out var exc) ? new Regex(exc) : null);
                return 0;

            case "report":
                Report.Run(opt["maps"].Split(',', StringSplitOptions.RemoveEmptyEntries), opt["hits"], opt["out"],
                    opt.GetValueOrDefault("title", "Condition coverage"),
                    opt.TryGetValue("scope", out var sc) ? new Regex(sc) : null);
                return 0;

            default:
                Console.Error.WriteLine(usage);
                return 2;
        }
    }
}
