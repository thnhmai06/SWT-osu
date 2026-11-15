using System.Reflection;
using System.Runtime.Loader;
using ConditionCoverage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace ConditionCoverage.Tests;

[TestFixture]
public class RewriterTests
{
    private static readonly List<MetadataReference> refs =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();

    private sealed record Built(Assembly Assembly, Registry Registry, string Source)
    {
        public (int t, int f) Hits(int id)
        {
            var probe = Assembly.GetType(Registry.ProbeName)!;
            var t = (int[])probe.GetField("t", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var f = (int[])probe.GetField("f", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            return (t[id], f[id]);
        }

        public object? Call(string type, string method, params object?[] args)
            => Assembly.GetType(type)!.GetMethod(method)!.Invoke(null, args);
    }

    private static Built build(string code)
    {
        var options = new CSharpParseOptions(LanguageVersion.Latest);
        var tree = CSharpSyntaxTree.ParseText(code, options);
        var comp = CSharpCompilation.Create("Snippet", [tree], refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        Assert.That(comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty, "snippet gốc phải hợp lệ");

        var registry = new Registry("Snippet");
        string rewritten = ConditionRewriter.Rewrite(comp.GetSemanticModel(tree), "Snippet.cs", registry).ToFullString();

        // phải parse lại từ text (giống khi ghi ra đĩa rồi build) để bắt lỗi in/parse ambiguity.
        var outComp = CSharpCompilation.Create("Snippet", [CSharpSyntaxTree.ParseText(rewritten, options), CSharpSyntaxTree.ParseText(ProbeSource.Generate("Snippet", registry.Conditions.Count), options)],
            refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        using var ms = new MemoryStream();
        var result = outComp.Emit(ms);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)) + "\n" + rewritten);

        ms.Position = 0;
        var asm = new AssemblyLoadContext(null, isCollectible: true).LoadFromStream(ms);
        return new Built(asm, registry, rewritten);
    }

    [Test]
    public void PatternAndOutVariablesStillDefinitelyAssigned()
    {
        var b = build("""
                      using System.Collections.Generic;
                      public static class S
                      {
                          public static int A(object o) => o is string s && s.Length > 1 ? s.Length : -1;
                          public static int B(Dictionary<int,int> d, int k) { if (d.TryGetValue(k, out var v) && v > 0) return v; return 0; }
                          public static int C(object o) { if (!(o is int i) || i < 0) return -1; return i; }
                      }
                      """);

        Assert.That(b.Call("S", "A", "xy"), Is.EqualTo(2));
        Assert.That(b.Call("S", "A", 5), Is.EqualTo(-1));
        Assert.That(b.Call("S", "B", new Dictionary<int, int> { [1] = 7 }, 1), Is.EqualTo(7));
        Assert.That(b.Call("S", "C", 3), Is.EqualTo(3));
    }

    [Test]
    public void ShortCircuitPreservedAndOutcomesCounted()
    {
        var b = build("""
                      public static class S
                      {
                          public static int Calls;
                          static bool Side() { Calls++; return true; }
                          public static bool And(bool a) => a && Side();
                          public static bool Or(bool a) { if (a || Side()) return true; return false; }
                      }
                      """);

        Assert.That(b.Registry.Conditions, Has.Count.EqualTo(4));
        Assert.That(b.Call("S", "And", false), Is.False);
        Assert.That(b.Assembly.GetType("S")!.GetField("Calls")!.GetValue(null), Is.EqualTo(0), "Side() không được gọi khi a=false");
        Assert.That(b.Hits(0), Is.EqualTo((0, 1)));
        Assert.That(b.Hits(1), Is.EqualTo((0, 0)), "toán hạng bị short-circuit chưa quan sát");

        Assert.That(b.Call("S", "And", true), Is.True);
        Assert.That(b.Hits(0), Is.EqualTo((1, 1)));
        Assert.That(b.Hits(1), Is.EqualTo((1, 0)));

        Assert.That(b.Call("S", "Or", true), Is.True);
        Assert.That(b.Hits(2), Is.EqualTo((1, 0)));
        Assert.That(b.Hits(3), Is.EqualTo((0, 0)));
    }

    [Test]
    public void ExpressionTreeLambdaConstantsAndNullableAreNotInstrumented()
    {
        var b = build("""
                      using System;
                      using System.Linq.Expressions;
                      public static class S
                      {
                          const bool K = true && false;
                          public static Expression<Func<int, bool>> E() => x => x > 1 && x < 5;
                          public static Func<int, bool> F() => x => x > 1 && x < 5;
                          public static bool N(bool? a, bool b) => (a ?? false) && b;
                          public static void D(bool p = true && false) { }
                      }
                      """);

        // chỉ F (2 điều kiện) và N (2 điều kiện); E, K, D không bị đụng.
        Assert.That(b.Registry.Conditions.Select(c => c.Member), Is.EquivalentTo(new[] { "F()", "F()", "N(bool?, bool)", "N(bool?, bool)" }));
        Assert.That(b.Registry.Conditions.Where(c => c.Member == "N(bool?, bool)").Select(c => c.Text), Is.EquivalentTo(new[] { "a ?? false", "b" }));
    }

    [Test]
    public void LoopsTernaryAndNestedDecisionsAreAttributed()
    {
        var b = build("""
                      namespace N.M
                      {
                          public class T
                          {
                              public int P { get { return Flag && Other ? 1 : 0; } }
                              public bool Flag = true, Other;
                              public static int Loop(int n) { int i = 0; while (i < n && i < 3) i++; for (; i < 10 && n > 0; i++) { } return i; }
                              public static bool Nested(System.Func<bool> f, bool a) => (f() && a) || (a && !f());
                          }
                      }
                      """);

        var byMember = b.Registry.Conditions.GroupBy(c => c.Member).ToDictionary(g => g.Key, g => g.Count());
        Assert.That(byMember["P.get"], Is.EqualTo(2));
        Assert.That(byMember["Loop(int)"], Is.EqualTo(4));
        Assert.That(byMember["Nested(System.Func<bool>, bool)"], Is.EqualTo(4));
        Assert.That(b.Registry.Conditions.All(c => c.Namespace == "N.M" && c.Type == "T"), Is.True);
        Assert.That(b.Call("N.M.T", "Loop", 5), Is.EqualTo(10));
    }

    [Test]
    public void ExternalDependenciesAreExcludedFromPureLogic()
    {
        var b = build("""
                      namespace osu.Framework.Graphics { public class Drawable { } public class Component : Drawable { } }
                      namespace osu.Framework.Platform { public class Storage { public bool Exists; } }
                      namespace osu.Framework.Allocation { public class ResolvedAttribute : System.Attribute { } }
                      namespace Game
                      {
                          public class Box : osu.Framework.Graphics.Drawable
                          {
                              public bool A(bool x, bool y) => x && y;
                              public class Inner { public bool B(bool x, bool y) => x || y; }
                          }
                          public class NonVisual : osu.Framework.Graphics.Component { public bool D(bool x, bool y) => x && y; }
                          public class Mixed
                          {
                              public bool Pure(bool x, bool y) => x && y;
                              public bool UsesHost(osu.Framework.Platform.Storage s, bool y) => s.Exists && y;
                          }
                          public class Injected { [osu.Framework.Allocation.Resolved] public bool Dep; public bool Pure(bool x, bool y) => x || y; }
                      }
                      """);

        string reason(string type, string member) => b.Registry.Conditions.First(c => c.Type == type && c.Member.StartsWith(member)).Exclusion;

        Assert.That(reason("Box", "A"), Is.EqualTo("UI"));
        Assert.That(reason("Box.Inner", "B"), Is.EqualTo("UI"));
        Assert.That(reason("NonVisual", "D"), Is.Empty, "Component không hiển thị là logic thuần");
        Assert.That(reason("Mixed", "Pure"), Is.Empty);
        Assert.That(reason("Mixed", "UsesHost"), Is.EqualTo("Host/IO"));
        Assert.That(reason("Injected", "Pure"), Is.EqualTo("DI"), "type có member [Resolved] bị loại cả type");
    }

    [Test]
    public void TypesNeedingDiRealmOrExternalConstructorArgsAreExcluded()
    {
        var b = build("""
                      namespace Realms { public class RealmObject { } }
                      namespace osu.Framework.Platform { public class Storage { } }
                      namespace osu.Framework.Allocation { public class ResolvedAttribute : System.Attribute { } }
                      namespace Game
                      {
                          public class WithDi { [osu.Framework.Allocation.Resolved] int x; public bool M(bool a, bool b) => a && b; }
                          public class Stored : Realms.RealmObject { public bool M(bool a, bool b) => a && b; }
                          public class NeedsStorage { public NeedsStorage(osu.Framework.Platform.Storage s) { } public bool M(bool a, bool b) => a && b; }
                          public class Constructible { public Constructible(int n) { } public bool M(bool a, bool b) => a && b; }
                          public class OneEasyCtor { public OneEasyCtor(osu.Framework.Platform.Storage s) { } public OneEasyCtor() { } public bool M(bool a, bool b) => a && b; }
                          public static class Util { public static bool M(bool a, bool b) => a && b; }
                          public class NeedsDiType { public NeedsDiType(WithDi d) { } public bool M(bool a, bool b) => a && b; }
                      }
                      """);

        string reason(string type) => b.Registry.Conditions.First(c => c.Type == type).Exclusion;

        Assert.That(reason("WithDi"), Is.EqualTo("DI"));
        Assert.That(reason("Stored"), Is.EqualTo("Database/Online"));
        Assert.That(reason("NeedsStorage"), Is.EqualTo("~Host/IO"), "tiền tố ~: nhóm bỏ qua hẳn khỏi báo cáo");
        Assert.That(reason("NeedsDiType"), Is.EqualTo("~DI"));
        Assert.That(reason("Constructible"), Is.Empty);
        Assert.That(reason("OneEasyCtor"), Is.Empty, "có ctor không cần thứ ngoài thì dựng được");
        Assert.That(reason("Util"), Is.Empty);
    }
}
