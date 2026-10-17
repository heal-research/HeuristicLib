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

        file sealed record ChildCrossover : Crossover<int>
        {
            public override ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory() => _ => new Executor();
            private sealed class Executor : CrossoverExecutor<int, SS, P>
            {
                public override IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => [];
            }
        }

        """;

    [Theory]
    [InlineData("ICrossover<int>", "Child.CreateExecutorFactory<SS, P>()(scope)")]
    [InlineData("ChildCrossover", "Child.CreateExecutorFactory()(scope)")]
    [InlineData("ChildCrossover", "(Child.CreateExecutorFactory())(scope)")]
    [InlineData("ChildCrossover", "Child.CreateExecutorFactory().Invoke(scope)")]
    public async Task FixedCode_Compiles_ForImmediateFactoryInvocation(string childType, string binding)
    {
        var fixedSource = await ApplyFixAsync(Preamble + $$"""
            file sealed record BypassingCrossover : Crossover<int>
            {
                public required {{childType}} Child { get; init; }
                public override ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory() => scope => {{binding}};
            }
            """);

        fixedSource.Replace(" ", "").ShouldContain("scope.Resolve<int,SS,P>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_WhenPreparationReturnsTheChildFactory()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record BypassingCrossover : Crossover<int>
            {
                public required ICrossover<int> Child { get; init; }
                public override ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory() => Child.CreateExecutorFactory<SS, P>();
            }
            """);

        fixedSource.Replace(" ", "").ShouldContain("scope.Resolve<int,SS,P>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForDeferredResolutionThroughAScopeField()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed class DeferredExecutor(ICrossover<int> child, ResolutionScope scope) : CrossoverExecutor<int, SS, P>
            {
                public override IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random, SS searchSpace, P problem) =>
                    Activate().Cross(parents, random, searchSpace, problem);
                private ICrossoverExecutor<int, SS, P> Activate() => child.CreateExecutorFactory<SS, P>()(scope.CreateChildScope());
            }
            """);

        fixedSource.Replace(" ", "").ShouldContain("scope.CreateChildScope().Resolve<int,SS,P>(child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForAFourArgumentRole()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record BypassingInterceptor : IConfigurationNode<IInterceptionExecutor<int, SS, P, PopulationState<int>>>
            {
                public required IInterceptor<int> Child { get; init; }
                public ExecutorFactory<IInterceptionExecutor<int, SS, P, PopulationState<int>>> CreateExecutorFactory() =>
                    scope => Child.CreateExecutorFactory<SS, P, PopulationState<int>>()(scope);
            }
            """);

        fixedSource.Replace(" ", "").ShouldContain("scope.Resolve<int,SS,P,PopulationState<int>>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForAConsumerDefinedExecutionContract()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file interface ICustomExecutor : IExecutor;
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
            file interface ICustomExecutor : IExecutor;
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
            file sealed record BypassingCrossover : Crossover<int>
            {
                public required ICrossover<int> Child { get; init; }
                public override ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory()
                {
                    var factory = Child.CreateExecutorFactory<SS, P>();
                    return scope => factory(scope);
                }
            }
            """, expectFix: false);
    }

    [Fact]
    public async Task DoesNotOfferAPartialFix_ForAGenericOwnerFactory()
    {
        await ApplyFixAsync(Preamble + """
            file sealed record GenericWrapper(IOperator<Mutation, int> Child) : WrappingOperator<Mutation, int>(Child)
            {
                protected override OperatorExecutorFactory CreateOperatorFactory() => Child.CreateExecutorFactory();
            }
            """, expectFix: false);
    }

    [Fact]
    public async Task FixedCode_Compiles_ForTheProtectedLeafFactoryHook()
    {
        var fixedSource = await ApplyFixAsync(Preamble + """
            file sealed record RecursiveLeaf : HEAL.HeuristicLib.Operators.Mutators.Mutator<int>
            {
                public required RecursiveLeaf Child { get; init; }
                protected override ExecutorFactory<IMutationExecutor<int, SS, P>> CreateFactory() =>
                    Child.CreateFactory();
            }
            """);
        fixedSource.Replace(" ", "").ShouldContain("scope.Resolve<int,SS,P>(Child)");
        await AssertCompilesWithoutDiagnosticAsync(fixedSource);
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
