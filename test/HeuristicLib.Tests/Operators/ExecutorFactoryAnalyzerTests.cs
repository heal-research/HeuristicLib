using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Operators.Crossovers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HEAL.HeuristicLib.Tests.Operators;

/// <summary>Compile real construction contracts before checking that building a child cannot silently bypass resolution.</summary>
public class ExecutorFactoryAnalyzerTests
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

        file record ChildCrossover : Crossover<int>
        {
            protected override ICrossoverExecutor<int, SS, P> CreateExecutor(ResolutionScope scope) => new Executor();
            private sealed class Executor : CrossoverExecutor<int, SS, P>
            {
                public override IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => [];
            }
        }

        file interface ICustomExecutor : IExecutor;

        """;

    [Theory]
    [InlineData("IOperator<Crossover, int>", "(ICrossoverExecutor<int, SS, P>)Child.CreateExecutor(scope.For<int, SS, P, ISearchState>())")]
    [InlineData("ChildCrossover", "(ICrossoverExecutor<int, SS, P>)((IOperator<Crossover, int>)Child).CreateExecutor(scope.For<int, SS, P, ISearchState>())")]
    public async Task Reports_WhenALeafHookCreatesItsChildDirectly(string childType, string construction)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record BypassingCrossover : Crossover<int>
            {
                public required {{childType}} Child { get; init; }
                protected override ICrossoverExecutor<int, SS, P> CreateExecutor(ResolutionScope scope) => {{construction}};
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Theory]
    [InlineData("Child.CreateExecutor(scope)")]
    [InlineData("Build(); IExecutor Build() => Child.CreateExecutor(scope)")]
    public async Task Reports_WhenARunGenericHookCreatesItsChildDirectly(string construction)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record BypassingCrossover : Operator<Crossover, int>
            {
                public required IOperator<Crossover, int> Child { get; init; }
                protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
                {
                    return {{construction}};
                }
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenAChildFactoryIsStoredDuringPreparation()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record CustomConfiguration : IConfigurationNode<ICustomExecutor>
            {
                public required IConfigurationNode<ICustomExecutor> Child { get; init; }
                public ExecutorFactory<ICustomExecutor> CreateExecutorFactory()
                {
                    var factory = Child.CreateExecutorFactory();
                    return scope => factory(scope);
                }
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DoesNotReport_WhenTheChildIsResolvedThroughTheScope()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record ResolvingCrossover : Crossover<int>
            {
                public required IOperator<Crossover, int> Child { get; init; }
                protected override ICrossoverExecutor<int, SS, P> CreateExecutor(ResolutionScope scope) => scope.Resolve<int, SS, P>(Child);
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task DoesNotReport_WhenTheCallSiteIsOutsideExecutionConstruction()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file static class DeliberateCaller
            {
                public static ICrossoverExecutor<int, SS, P> Create(IOperator<Crossover, int> crossover) => (ICrossoverExecutor<int, SS, P>)crossover.CreateExecutor(ResolutionScope.Create().For<int, SS, P, ISearchState>());
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("CreateExecutor(scope, 1)")]
    [InlineData("this.CreateExecutor(scope, 1)")]
    [InlineData("base.CreateExecutor(scope)")]
    public async Task DoesNotReport_WhenAHookDelegatesToItselfOrItsBase(string construction)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record DelegatingCrossover : ChildCrossover
            {
                protected override ICrossoverExecutor<int, SS, P> CreateExecutor(ResolutionScope scope) => {{construction}};
                private ICrossoverExecutor<int, SS, P> CreateExecutor(ResolutionScope scope, int unused) => base.CreateExecutor(scope);
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reports_WhenTheBypassIsInsideAnExplicitInterfaceImplementation()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record BridgingCrossover : IOperator<Crossover, int>
            {
                public required IOperator<Crossover, int> Child { get; init; }
                IExecutor IOperator<Crossover, int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
                    Child.CreateExecutor(scope);
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DoesNotReport_WhenAnExplicitBridgeCallsItsOwnHook()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record BridgingCrossover : ChildCrossover, IOperator<Crossover, int>
            {
                IExecutor IOperator<Crossover, int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
                    CreateExecutor(scope.Scope);
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reports_WhenTheChildSharesItsHolderType()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record RecursiveCrossover : ChildCrossover
            {
                public RecursiveCrossover? Child { get; init; }
                protected override ICrossoverExecutor<int, SS, P> CreateExecutor(ResolutionScope scope) =>
                    Child is null ? base.CreateExecutor(scope) : Child.CreateExecutor(scope);
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenTheConstructionIsDeferredToAnExecutionMethod()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed class DeferredExecutor(IOperator<Crossover, int> child, ResolutionScope scope) : CrossoverExecutor<int, SS, P>
            {
                public override IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random, SS searchSpace, P problem) =>
                    Activate().Cross(parents, random, searchSpace, problem);
                private ICrossoverExecutor<int, SS, P> Activate() => (ICrossoverExecutor<int, SS, P>)child.CreateExecutor(scope.For<int, SS, P, ISearchState>());
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Theory]
    [InlineData("source.CreateExecutorFactory()", 0)]
    [InlineData("Other.CreateExecutorFactory()", 1)]
    public async Task ResolutionPreparation_OnlyAllowsItsOwnSource(string preparation, int count)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file static class CustomResolution
            {
                public static ICustomExecutor Resolve(ResolutionScope scope, IConfigurationNode<ICustomExecutor> configuration, IConfigurationNode<ICustomExecutor> Other) =>
                    scope.Resolve(configuration, source => { var factory = {{preparation}}; return new ExecutorFactory<ICustomExecutor>(innerScope => factory(innerScope)); },
                        static (_, _) => throw new System.InvalidOperationException());
            }
            """);
        diagnostics.Length.ShouldBe(count);
    }

    [Fact]
    public async Task Reports_WhenPreparationOfTheSourceIsDeferredInsideAFactoryCallback()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file static class CustomResolution
            {
                public static ICustomExecutor Resolve(ResolutionScope scope, IConfigurationNode<ICustomExecutor> configuration) =>
                    scope.Resolve<IConfigurationNode<ICustomExecutor>, ICustomExecutor>(configuration, source => innerScope => source.CreateExecutorFactory()(innerScope),
                        static (_, _) => throw new System.InvalidOperationException());
            }
            """);
        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenAWrapperCreatesItsChildDirectly()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record CustomWrapper(IOperator<Crossover, int> Child) : WrappingOperator<Crossover, int>(Child)
            {
                protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
                {
                    _ = Child.CreateExecutor(scope);
                    return scope.ResolveOperator(ChildOperator);
                }
            }
            """);
        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_ForAConsumerDefinedExecutionContract()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record CustomConfiguration : IConfigurationNode<ICustomExecutor>
            {
                public required IConfigurationNode<ICustomExecutor> Child { get; init; }
                public ExecutorFactory<ICustomExecutor> CreateExecutorFactory() => scope => Child.CreateExecutorFactory()(scope);
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DoesNotReport_UnrelatedMethodsWithMatchingNames()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed class UnrelatedChild
            {
                public System.Func<ResolutionScope, int> CreateExecutorFactory() => _ => 0;
                public int CreateExecutor(ResolutionScope scope) => 0;
            }
            file sealed class DeliberateCaller
            {
                public int Build(ResolutionScope scope) => new UnrelatedChild().CreateExecutorFactory()(scope) + new UnrelatedChild().CreateExecutor(scope);
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reports_WhenASameTypeChildBypassesResolutionThroughTheProtectedHook()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record RecursiveLeaf : HEAL.HeuristicLib.Operators.Mutators.Mutator<int>
            {
                public RecursiveLeaf? Child { get; init; }
                protected override IMutationExecutor<int, SS, P> CreateExecutor(ResolutionScope scope) =>
                    Child is null ? throw new System.InvalidOperationException() : Child.CreateExecutor(scope);
            }
            """);
        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        var platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            ?? [];

        var compilation = CSharpCompilation.Create(
            assemblyName: "ExecutionFactoryAnalyzerTest",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))],
            references: platformAssemblies.Append(MetadataReference.CreateFromFile(typeof(Crossover<>).Assembly.Location)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostics = await compilation.WithAnalyzers([new ExecutorFactoryAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        diagnostics.ShouldNotContain(static diagnostic => diagnostic.Id == "AD0001");
        return [.. diagnostics.Where(static diagnostic => diagnostic.Id == ExecutorFactoryAnalyzer.DiagnosticId)];
    }
}
