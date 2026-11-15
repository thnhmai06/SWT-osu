using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ConditionCoverage;

public static class Report
{
    private sealed record Hit(string Assembly, ConditionInfo Cond, long T, long F)
    {
        public int Outcomes => (T > 0 ? 1 : 0) + (F > 0 ? 1 : 0);
        public bool Excluded => Cond.Exclusion.Length > 0;
    }

    private readonly record struct Agg(int Conditions, int Outcomes, int Full, int Unreached)
    {
        public double Percent => Conditions == 0 ? 0 : 100.0 * Outcomes / (2 * Conditions);
        public static Agg Of(IEnumerable<Hit> hits)
        {
            int c = 0, o = 0, f = 0, u = 0;
            foreach (var h in hits)
            {
                c++;
                o += h.Outcomes;
                if (h.Outcomes == 2) f++;
                if (h.Outcomes == 0) u++;
            }

            return new Agg(c, o, f, u);
        }

        public string Row() => $"{Conditions} | {Outcomes}/{2 * Conditions} | {Full} | {Unreached} | {Percent:0.0}%";
    }

    private const string header = "| Điều kiện | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage |";

    public static void Run(string[] mapPaths, string hitsDir, string outDir, string title, Regex? scope)
    {
        Directory.CreateDirectory(outDir);

        var hitsByAsm = new Dictionary<string, (long[] t, long[] f)>();
        var maps = mapPaths.Select(p => JsonSerializer.Deserialize<InstrumentMap>(File.ReadAllText(p))!).ToList();
        foreach (var m in maps) hitsByAsm[m.Assembly] = (new long[m.Conditions.Count + 1], new long[m.Conditions.Count + 1]);

        int dumpFiles = 0;
        foreach (var m in maps)
        {
            foreach (var file in Directory.EnumerateFiles(hitsDir, $"hits-{m.Assembly}-*.txt"))
            {
                // tên file có dạng hits-<asm>-<pid>.txt; tránh nhầm assembly có prefix trùng nhau
                if (!Regex.IsMatch(Path.GetFileName(file), "^hits-" + Regex.Escape(m.Assembly) + @"-\d+\.txt$")) continue;
                dumpFiles++;
                var (t, f) = hitsByAsm[m.Assembly];
                foreach (var line in File.ReadLines(file))
                {
                    var p = line.Split(',');
                    if (p.Length != 3) continue;
                    int id = int.Parse(p[0]);
                    if (id < t.Length) { t[id] += long.Parse(p[1]); f[id] += long.Parse(p[2]); }
                }
            }
        }

        var all = new List<Hit>();
        foreach (var m in maps)
        {
            var (t, f) = hitsByAsm[m.Assembly];
            foreach (var c in m.Conditions)
            {
                if (scope != null && !scope.IsMatch($"{c.Namespace}.{c.Type}.{c.Member}")) continue;
                all.Add(new Hit(m.Assembly, c, t[c.Id], f[c.Id]));
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# {title}");
        sb.AppendLine();
        sb.AppendLine("## Phương pháp");
        sb.AppendLine();
        sb.AppendLine("- Công cụ: `tools/ConditionCoverage` (Roslyn). Mỗi *điều kiện nguyên tử* (toán hạng lá của cây `&&`, `||`, `!`, `&`, `|` trong `if`/`while`/`do`/`for`/`?:` hoặc biểu thức logic bất kỳ) được bọc probe ghi nhận kết quả `true`/`false`.");
        sb.AppendLine("- **Condition coverage = số outcome đã quan sát / (2 × số điều kiện)**: mỗi điều kiện cần từng nhận `true` và từng nhận `false`.");
        sb.AppendLine("- Điều kiện bị **short-circuit** (không được đánh giá) được tính là *chưa quan sát*, không tính là `false`.");
        sb.AppendLine("- Không đo: điều kiện hằng, trong attribute/giá trị mặc định tham số, và trong lambda chuyển thành `Expression<T>` (giữ nguyên để LINQ provider dịch được).");
        sb.AppendLine("- Branch coverage không nằm trong báo cáo này (dùng coverlet/dotCover); đây là chỉ số bổ sung ở mức điều kiện.");
        sb.AppendLine($"- Số file hits đã gộp: {dumpFiles}.");
        sb.AppendLine();
        sb.AppendLine("## Solution");
        sb.AppendLine();
        sb.AppendLine("| Project |" + header[1..]);
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var g in all.GroupBy(h => h.Assembly).OrderBy(g => g.Key))
            sb.AppendLine($"| [{g.Key}](condition-coverage-{g.Key}.md) | {Agg.Of(g).Row()} |");
        sb.AppendLine($"| **Tổng** | {Agg.Of(all).Row()} |");
        sb.AppendLine();
        sb.AppendLine("## Solution — chỉ tính logic thuần (loại code phụ thuộc ngoài)");
        sb.AppendLine();
        sb.AppendLine("Logic thuần = điều kiện nằm trong code **không phụ thuộc** vào thứ ngoài code đó (không cần UI, input thật, game host, IO/audio/texture, database/online, hay DI của framework). Bị loại, theo thứ tự ưu tiên:");
        sb.AppendLine();
        sb.AppendLine("| Nhóm loại | Nhận diện (tự động bằng Roslyn) |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| TestInfra | type trong namespace `osu.Game.Tests` (scaffold test nằm trong osu.Game) |");
        sb.AppendLine("| UI | type dẫn xuất từ `Drawable` (không qua `Component`); hoặc member tham chiếu type dẫn xuất từ `Drawable` (không qua `Component`) |");
        sb.AppendLine("| Host/IO | type kế thừa `osu.Framework.Game`/`Platform`; hoặc member dùng `osu.Framework.Platform|IO|Audio|Threading`, texture/shader/render, `osu.Game.Skinning|Audio|IO` |");
        sb.AppendLine("| Input | type kế thừa/triển khai `osu.Framework.Input.*`; hoặc member dùng `osu.Framework.Input`, `osu.Game.Input` |");
        sb.AppendLine("| Database/Online | member dùng `Realms`, `osu.Game.Database`, `osu.Game.Online`, `System.Net` |");
        sb.AppendLine("| DI | member có `[Resolved]`/`[BackgroundDependencyLoader]`/`[Cached]` hoặc dùng symbol có `[Resolved]` |");
        sb.AppendLine();
        sb.AppendLine("Việc loại làm theo **member** (không loại cả type nếu chỉ một số member phụ thuộc ngoài). Đây là heuristic dựa trên kiểu được tham chiếu, không phải phân tích luồng dữ liệu: member gọi gián tiếp qua method khác vẫn được coi là thuần nếu bản thân nó không nhắc tới type ngoài. Nếu cả target đo (vd. method dùng Realm) phụ thuộc ngoài thì phần logic thuần của target là 0 điều kiện; xem cột \"toàn bộ\" để đối chiếu.");
        sb.AppendLine();
        sb.AppendLine("| Project | Tổng điều kiện | UI | Input | Host/IO | Database/Online | DI | TestInfra | Logic thuần còn lại | Outcome đã phủ | Phủ đủ cả T/F | Chưa được đánh giá | Condition coverage (logic thuần) | Condition coverage (toàn bộ) |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var g in all.GroupBy(h => h.Assembly).OrderBy(g => g.Key))
            sb.AppendLine(Compare($"[{g.Key}](condition-coverage-{g.Key}.md)", g));
        sb.AppendLine(Compare("**Tổng**", all));
        File.WriteAllText(Path.Combine(outDir, "condition-coverage.md"), sb.ToString());

        foreach (var g in all.GroupBy(h => h.Assembly))
            File.WriteAllText(Path.Combine(outDir, $"condition-coverage-{g.Key}.md"), Project(g.Key, g.ToList()));

        var total = Agg.Of(all);
        Console.WriteLine($"TOTAL conditions={total.Conditions} outcomes={total.Outcomes}/{2 * total.Conditions} full={total.Full} unreached={total.Unreached} coverage={total.Percent:0.0}%");
    }

    private static readonly string[] reasons = ["UI", "Input", "Host/IO", "Database/Online", "DI", "TestInfra"];

    private static string Compare(string name, IEnumerable<Hit> hits)
    {
        var list = hits.ToList();
        var logic = Agg.Of(list.Where(h => !h.Excluded));
        string byReason = string.Join(" | ", reasons.Select(r => list.Count(h => h.Cond.Exclusion == r)));
        return $"| {name} | {list.Count} | {byReason} | {logic.Conditions} | {logic.Outcomes}/{2 * logic.Conditions} | {logic.Full} | {logic.Unreached} | {logic.Percent:0.0}% | {Agg.Of(list).Percent:0.0}% |";
    }

    private static string esc(string s) => "`" + s.Replace("`", "'") + "`";
    private static string cell(string s) => s.Replace("|", "\\|");

    private static string Project(string asm, List<Hit> hits)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Condition coverage — {asm}");
        sb.AppendLine();
        sb.AppendLine($"Tổng: {Agg.Of(hits).Row()}");
        sb.AppendLine();

        sb.AppendLine("## Namespace");
        sb.AppendLine();
        sb.AppendLine("| Namespace |" + header[1..]);
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var g in hits.GroupBy(h => h.Cond.Namespace).OrderBy(g => g.Key, StringComparer.Ordinal))
            sb.AppendLine($"| {cell(g.Key)} | {Agg.Of(g).Row()} |");

        var logicHits = hits.Where(h => !h.Excluded).ToList();
        sb.AppendLine();
        sb.AppendLine("## Namespace — chỉ tính logic thuần");
        sb.AppendLine();
        string excl = string.Join(", ", reasons.Select(r => $"{r}: {hits.Count(h => h.Cond.Exclusion == r)}"));
        sb.AppendLine($"Loại {hits.Count - logicHits.Count} điều kiện phụ thuộc ngoài ({excl}). Còn lại: {Agg.Of(logicHits).Row()}");
        sb.AppendLine();
        sb.AppendLine("| Namespace |" + header[1..]);
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var g in logicHits.GroupBy(h => h.Cond.Namespace).OrderBy(g => g.Key, StringComparer.Ordinal))
            sb.AppendLine($"| {cell(g.Key)} | {Agg.Of(g).Row()} |");

        sb.AppendLine();
        sb.AppendLine("## Type");
        sb.AppendLine();
        sb.AppendLine("| Type |" + header[1..]);
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var g in hits.GroupBy(h => (h.Cond.Namespace, h.Cond.Type)).OrderBy(g => g.Key.Namespace, StringComparer.Ordinal).ThenBy(g => g.Key.Type, StringComparer.Ordinal))
            sb.AppendLine($"| {cell(g.Key.Namespace + "." + g.Key.Type)}{(g.All(h => h.Excluded) ? " _(loại)_" : "")} | {Agg.Of(g).Row()} |");

        sb.AppendLine();
        sb.AppendLine("## Method");
        sb.AppendLine();
        foreach (var tg in hits.GroupBy(h => (h.Cond.Namespace, h.Cond.Type)).OrderBy(g => g.Key.Namespace, StringComparer.Ordinal).ThenBy(g => g.Key.Type, StringComparer.Ordinal))
        {
            sb.AppendLine($"### {tg.Key.Namespace}.{tg.Key.Type}");
            sb.AppendLine();
            sb.AppendLine("| Method |" + header[1..]);
            sb.AppendLine("|---|---|---|---|---|---|");
            var methods = tg.GroupBy(h => h.Cond.Member).OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
            foreach (var mg in methods)
                sb.AppendLine($"| {cell(esc(mg.Key))} | {Agg.Of(mg).Row()} |");
            sb.AppendLine();

            var missing = methods.Select(mg => (mg.Key, Miss: mg.Where(h => h.Outcomes < 2).OrderBy(h => h.Cond.File, StringComparer.Ordinal).ThenBy(h => h.Cond.Line).ThenBy(h => h.Cond.Id).ToList())).Where(x => x.Miss.Count > 0).ToList();
            if (missing.Count == 0) { sb.AppendLine("_Mọi điều kiện đã phủ đủ cả true và false._"); sb.AppendLine(); continue; }

            sb.AppendLine("<details><summary>Điều kiện còn thiếu</summary>");
            sb.AppendLine();
            foreach (var (member, miss) in missing)
            {
                sb.AppendLine($"**{cell(esc(member))}**");
                sb.AppendLine();
                foreach (var h in miss)
                {
                    string what = h.Outcomes == 0 ? "chưa được đánh giá" : h.T == 0 ? "thiếu `true`" : "thiếu `false`";
                    sb.AppendLine($"- `{h.Cond.File}:{h.Cond.Line}` {esc(h.Cond.Text)} — {what}");
                }

                sb.AppendLine();
            }

            sb.AppendLine("</details>");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
