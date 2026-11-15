using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace ConditionCoverage;

public static class Instrumenter
{
    /// <summary>Ghi đè file .cs trong project (nên chạy trên worktree tách riêng) và xuất map.</summary>
    public static async Task<InstrumentMap> RunAsync(string csproj, string mapPath, Regex? include, Regex? exclude)
    {
        using var ws = MSBuildWorkspace.Create(new Dictionary<string, string> { ["Configuration"] = "Debug" });
        ws.RegisterWorkspaceFailedHandler(e => { if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure && !e.Diagnostic.Message.Contains("known")) Console.Error.WriteLine("  [ws] " + e.Diagnostic.Message); });

        var project = await ws.OpenProjectAsync(Path.GetFullPath(csproj));
        string projDir = Path.GetDirectoryName(project.FilePath)!;
        string asm = project.AssemblyName;
        var reg = new Registry(asm);
        int files = 0;

        foreach (var doc in project.Documents)
        {
            string? path = doc.FilePath;
            if (path == null || !path.StartsWith(projDir + Path.DirectorySeparatorChar) || !path.EndsWith(".cs")) continue;

            string rel = Path.GetRelativePath(projDir, path).Replace('\\', '/');
            if (rel.StartsWith("obj/") || rel.StartsWith("bin/") || rel.EndsWith(".g.cs") || rel.EndsWith(".Designer.cs")) continue;
            if (include != null && !include.IsMatch(rel)) continue;
            if (exclude != null && exclude.IsMatch(rel)) continue;

            var model = await doc.GetSemanticModelAsync();
            if (model == null) continue;

            int before = reg.Conditions.Count;
            var root = ConditionRewriter.Rewrite(model, rel, reg);
            if (reg.Conditions.Count == before) continue;

            File.WriteAllText(path, root.ToFullString());
            files++;
        }

        File.WriteAllText(Path.Combine(projDir, "__CondCov.g.cs"), ProbeSource.Generate(asm, reg.Conditions.Count));

        var map = new InstrumentMap { Assembly = asm, Conditions = reg.Conditions, Decisions = reg.Decisions };
        File.WriteAllText(mapPath, JsonSerializer.Serialize(map));
        Console.WriteLine($"{asm}: {files} file, {reg.Decisions.Count} decision, {reg.Conditions.Count} condition");
        return map;
    }
}
