using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Analyzers.CodeFixes;
using HEAL.HeuristicLib.Operators.Crossovers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HEAL.HeuristicLib.Tests.Operators;

/// <summary>Apply the offered fix and compile it against the real library, including custom execution contracts.</summary>
public class ChildResolutionCodeFixTests
{
    private const string Preamble = """
        using System.Collections.Generic;
        using HEAL.HeuristicLib.Algorithms;
        using HEAL.HeuristicLib.Execution;
        using HEAL.HeuristicLib.Operators;
        using HEAL.HeuristicLib.Operators.Crossovers;
        using HEAL.HeuristicLib.Problems;
        using HEAL.HeuristicLib.Random;
        using HEAL.HeuristicLib.SearchSpaces;
        using SS = HEAL.HeuristicLib.SearchSpaces.ISearchSpace<int>;
        using P = HEAL.HeuristicLib.Problems.IProblem<int, HEAL.HeuristicLib.SearchSpaces.ISearchSpace<int>>;

        file interface ICustomExecutor : IExecutor;

        """;

    [Fact]
    public async Task FixedCode_Compiles_ForAConsumerDefinedRole()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record CustomConfiguration : IConfigurationNode<ICustomExecutor>
            {
                public required IConfigurationNode<ICustomExecutor> Child { get; init; }
                public ICustomExecutor CreateExecutor(ResolutionScope scope) => Child.CreateExecutor(scope);
            }
            """);

        fixedSource.ShouldContain("=> scope.Resolve(Child);");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForDeferredResolutionThroughAScopeField()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed class DeferredExecutor(IConfigurationNode<ICustomExecutor> child, ResolutionScope scope) : ICustomExecutor
            {
                public ICustomExecutor Activate() => child.CreateExecutor(scope.CreateChildScope());
            }
            """);

        fixedSource.Replace(" ", "").ShouldContain("scope.CreateChildScope().Resolve(child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForTheProtectedLeafHook()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record RecursiveLeaf : HEAL.HeuristicLib.Operators.Mutators.Mutator<int>
            {
                public required RecursiveLeaf Child { get; init; }
                protected override IMutationExecutor<int, SS, P> CreateExecutor(ResolutionScope scope) =>
                    Child.CreateExecutor(scope);
            }
            """);
        fixedSource.Replace(" ", "").ShouldContain("scope.Resolve<int,SS,P>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForAFourArgumentRole()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record BypassingTerminator : HEAL.HeuristicLib.Operators.Terminators.Terminator<int, SS, P, PopulationState<int>>
            {
                public required BypassingTerminator Child { get; init; }
                protected override ITerminationExecutor<int, SS, P, PopulationState<int>> CreateExecutor(ResolutionScope scope) =>
                    Child.CreateExecutor(scope);
            }
            """);

        fixedSource.Replace(" ", "").ShouldContain("scope.Resolve<int,SS,P,PopulationState<int>>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForARunGenericHook()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record ForwardingCrossover : Operator<Crossover, int>
            {
                public required IOperator<Crossover, int> Child { get; init; }
                protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
                    Child.CreateExecutor(scope);
            }
            """);

        fixedSource.ShouldContain("scope.ResolveOperator(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Theory]
    [InlineData("Algorithm<BypassingAlgorithm, int, PopulationState<int>>", "<TSearchSpace, TProblem>", "TSearchSpace, TProblem")]
    [InlineData("Algorithm<BypassingAlgorithm, int, SS, P, PopulationState<int>>", "", "SS, P")]
    public async Task FixedCode_Compiles_ForAnAlgorithmHook(string algorithmBase, string hookParameters, string runTypes)
    {
        var fixedSource = await ApplyFixAsync(Preamble + $$"""
            file sealed record BypassingAlgorithm : {{algorithmBase}}
            {
                public required IAlgorithm<int, PopulationState<int>> Child { get; init; }
                protected override IAlgorithmExecutor<int, {{runTypes}}, PopulationState<int>> CreateExecutor{{hookParameters}}(ResolutionScope<int, {{runTypes}}, PopulationState<int>> scope) =>
                    Child.CreateExecutor(scope);
            }
            """);

        fixedSource.ShouldContain("scope.Resolve(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForAConsumerDefinedRoleInterface()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file interface ICustomRole<T> : IConfigurationNode<CustomExecutor<T>>;
            file sealed class CustomExecutor<T> : IExecutor;
            file sealed record CustomConfiguration : ICustomRole<int>
            {
                public required ICustomRole<int> Child { get; init; }
                public CustomExecutor<int> CreateExecutor(ResolutionScope scope) => Child.CreateExecutor(scope);
            }
            """);

        fixedSource.ShouldContain("=> scope.Resolve(Child);");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task DoesNotOfferAnUncompilableFix_WhenACustomRoleIsNotResolvable()
    {
        await ApplyFixAsync(Preamble + """
            file interface ICustomRole : IConfigurationNode
            {
                ICustomExecutor CreateExecutor(ResolutionScope scope);
            }
            file sealed record CustomConfiguration : ICustomRole
            {
                public required ICustomRole Child { get; init; }
                public ICustomExecutor CreateExecutor(ResolutionScope scope) => Child.CreateExecutor(scope);
            }
            """, expectFix: false);
    }

    private static async Task<string> ApplyFixAsync(string source, bool expectFix = true)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("CodeFixTest", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))
            .AddMetadataReferences(References);
        var document = project.AddDocument("Input.cs", SourceText.From(source));
        var compilation = await document.Project.GetCompilationAsync();
        compilation.ShouldNotBeNull();
        compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostics = await compilation.WithAnalyzers([new ChildResolutionAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        diagnostics.ShouldNotContain(static diagnostic => diagnostic.Id == "AD0001");
        var diagnostic = diagnostics.Single(d => d.Id == ChildResolutionAnalyzer.DiagnosticId);

        CodeAction? registered = null;
        var context = new CodeFixContext(document, diagnostic, (action, _) => registered = action, CancellationToken.None);
        await new ChildResolutionCodeFixProvider().RegisterCodeFixesAsync(context);
        if (!expectFix)
        {
            registered.ShouldBeNull();
            return source;
        }
        registered.ShouldNotBeNull("the fix provider offered no action");
        var operations = await registered.GetOperationsAsync(CancellationToken.None);
        var changed = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id);
        changed.ShouldNotBeNull();
        return (await changed.GetTextAsync()).ToString();
    }

    private static async Task AssertCompilesWithoutDiagnosticAsync(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "CodeFixVerification",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))],
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var remaining = await compilation.WithAnalyzers([new ChildResolutionAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        remaining.ShouldNotContain(static diagnostic => diagnostic.Id == "AD0001");
        remaining.Where(static diagnostic => diagnostic.Id == ChildResolutionAnalyzer.DiagnosticId).ShouldBeEmpty();
    }

    private static IReadOnlyList<MetadataReference> References { get; } =
    [
        .. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path)) ?? [],
        MetadataReference.CreateFromFile(typeof(Crossover<>).Assembly.Location)
    ];
}
