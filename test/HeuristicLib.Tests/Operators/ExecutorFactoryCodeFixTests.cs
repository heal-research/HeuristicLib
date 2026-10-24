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
public class ExecutorFactoryCodeFixTests
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

    [Theory]
    [InlineData("Child.CreateExecutorFactory()(scope)")]
    [InlineData("(Child.CreateExecutorFactory())(scope)")]
    [InlineData("Child.CreateExecutorFactory().Invoke(scope)")]
    public async Task FixedCode_Compiles_ForImmediateFactoryInvocation(string binding)
    {
        var fixedSource = await ApplyFixAsync(Preamble + $$"""
            file sealed record CustomConfiguration : IConfigurationNode<ICustomExecutor>
            {
                public required IConfigurationNode<ICustomExecutor> Child { get; init; }
                public ExecutorFactory<ICustomExecutor> CreateExecutorFactory() => scope => {{binding}};
            }
            """);
        fixedSource.ShouldContain("scope => scope.Resolve(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_WhenPreparationReturnsTheChildFactory()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record CustomConfiguration : IConfigurationNode<ICustomExecutor>
            {
                public required IConfigurationNode<ICustomExecutor> Child { get; init; }
                public ExecutorFactory<ICustomExecutor> CreateExecutorFactory() => Child.CreateExecutorFactory();
            }
            """);

        fixedSource.ShouldContain("scope => scope.Resolve(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForDeferredResolutionThroughAScopeField()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed class DeferredExecutor(IConfigurationNode<ICustomExecutor> child, ResolutionScope scope) : ICustomExecutor
            {
                public ICustomExecutor Activate() => child.CreateExecutorFactory()(scope.CreateChildScope());
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

    [Fact]
    public async Task FixedCode_Compiles_ForAConsumerDefinedExecutionContract()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record CustomConfiguration : IConfigurationNode<ICustomExecutor>
            {
                public required IConfigurationNode<ICustomExecutor> Child { get; init; }
                public ExecutorFactory<ICustomExecutor> CreateExecutorFactory() => scope => Child.CreateExecutorFactory()(scope);
            }
            """);

        fixedSource.ShouldContain("scope.Resolve(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForAConsumerDefinedRoleAndResolver()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file interface ICustomRole<T> : IConfigurationNode
            {
                ExecutorFactory<CustomExecutor<T>> CreateExecutorFactory();
            }
            file sealed class CustomExecutor<T> : IExecutor;
            file sealed record CustomConfiguration : ICustomRole<int>
            {
                public required ICustomRole<int> Child { get; init; }
                public ExecutorFactory<CustomExecutor<int>> CreateExecutorFactory() => scope => Child.CreateExecutorFactory()(scope);
            }
            file static class CustomResolution
            {
                public static CustomExecutor<T> Resolve<T>(this ResolutionScope scope, ICustomRole<T> source) =>
                    scope.Resolve(source, static configuration => configuration.CreateExecutorFactory(),
                        static (_, _) => throw new System.InvalidOperationException());
            }
            """);

        fixedSource.ShouldContain("scope.Resolve<int>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task DoesNotOfferAnUncompilableFix_WhenACustomRoleHasNoResolver()
    {
        await ApplyFixAsync(Preamble + """
            file interface ICustomRole : IConfigurationNode
            {
                ExecutorFactory<ICustomExecutor> CreateExecutorFactory();
            }
            file sealed record CustomConfiguration : ICustomRole
            {
                public required ICustomRole Child { get; init; }
                public ExecutorFactory<ICustomExecutor> CreateExecutorFactory() => scope => Child.CreateExecutorFactory()(scope);
            }
            """, expectFix: false);
    }

    [Fact]
    public async Task DoesNotOfferAPartialFix_ForAStoredFactory()
    {
        await ApplyFixAsync(Preamble + """
            file sealed record CustomConfiguration : IConfigurationNode<ICustomExecutor>
            {
                public required IConfigurationNode<ICustomExecutor> Child { get; init; }
                public ExecutorFactory<ICustomExecutor> CreateExecutorFactory()
                {
                    var factory = Child.CreateExecutorFactory();
                    return scope => factory(scope);
                }
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
        var diagnostics = await compilation.WithAnalyzers([new ExecutorFactoryAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        diagnostics.ShouldNotContain(static diagnostic => diagnostic.Id == "AD0001");
        var diagnostic = diagnostics.Single(d => d.Id == ExecutorFactoryAnalyzer.DiagnosticId);

        CodeAction? registered = null;
        var context = new CodeFixContext(document, diagnostic, (action, _) => registered = action, CancellationToken.None);
        await new ExecutorFactoryCodeFixProvider().RegisterCodeFixesAsync(context);
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
        var remaining = await compilation.WithAnalyzers([new ExecutorFactoryAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        remaining.ShouldNotContain(static diagnostic => diagnostic.Id == "AD0001");
        remaining.Where(static diagnostic => diagnostic.Id == ExecutorFactoryAnalyzer.DiagnosticId).ShouldBeEmpty();
    }

    private static IReadOnlyList<MetadataReference> References { get; } =
    [
        .. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path)) ?? [],
        MetadataReference.CreateFromFile(typeof(Crossover<>).Assembly.Location)
    ];
}
