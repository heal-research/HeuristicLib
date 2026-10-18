using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Operators.Crossovers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HEAL.HeuristicLib.Tests.Operators;

/// <summary>Compile real factory contracts before checking that child preparation cannot silently bypass resolution.</summary>
public class ExecutorFactoryAnalyzerTests
{
    private const string Preamble = """
        using System.Collections.Generic;
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
    public async Task Reports_WhenAChildFactoryIsInvokedInsideTheBinding(string childType, string binding)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record BypassingCrossover : Crossover<int>
            {
                public required {{childType}} Child { get; init; }
                public override ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory() => scope => {{binding}};
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Theory]
    [InlineData("Child.CreateExecutorFactory<SS, P>()")]
    [InlineData("Prepare(); ExecutorFactory<ICrossoverExecutor<int, SS, P>> Prepare() => Child.CreateExecutorFactory<SS, P>()")]
    public async Task Reports_WhenAChildFactoryIsPreparedDirectly(string preparation)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record BypassingCrossover : Crossover<int>
            {
                public required ICrossover<int> Child { get; init; }
                public override ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory()
                {
                    return {{preparation}};
                }
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenAChildFactoryIsStoredDuringPreparation()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record BypassingCrossover : Crossover<int>
            {
                public required ICrossover<int> Child { get; init; }
                public override ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory()
                {
                    var factory = Child.CreateExecutorFactory<SS, P>();
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
                public required ICrossover<int> Child { get; init; }
                public override ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory() => scope => scope.Resolve<int, SS, P>(Child);
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
                public static ICrossoverExecutor<int, SS, P> Create(ChildCrossover mutator) => mutator.CreateExecutorFactory()(ResolutionScope.Create());
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("CreateExecutorFactory(1)")]
    [InlineData("this.CreateExecutorFactory(1)")]
    [InlineData("base.CreateExecutorFactory()")]
    public async Task DoesNotReport_WhenAFactoryHookDelegatesToItselfOrItsBase(string preparation)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record DelegatingCrossover : ChildCrossover
            {
                public override ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory() => {{preparation}};
                private ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory(int unused) => base.CreateExecutorFactory();
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reports_WhenTheBypassIsInsideAnExplicitInterfaceImplementation()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record BridgingCrossover : ICrossover<int>
            {
                public required ChildCrossover Child { get; init; }
                ExecutorFactory<ICrossoverExecutor<int, TRunSearchSpace, TRunProblem>> ICrossover<int>.CreateExecutorFactory<TRunSearchSpace, TRunProblem>() =>
                    scope => (ICrossoverExecutor<int, TRunSearchSpace, TRunProblem>)Child.CreateExecutorFactory()(scope);
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DoesNotReport_WhenAnExplicitBridgeCallsItsOwnHook()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record BridgingCrossover : ChildCrossover, ICrossover<int>
            {
                ExecutorFactory<ICrossoverExecutor<int, TRunSearchSpace, TRunProblem>> ICrossover<int>.CreateExecutorFactory<TRunSearchSpace, TRunProblem>() =>
                    (ExecutorFactory<ICrossoverExecutor<int, TRunSearchSpace, TRunProblem>>)CreateExecutorFactory();
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
                public override ExecutorFactory<ICrossoverExecutor<int, SS, P>> CreateExecutorFactory() =>
                    Child is null ? base.CreateExecutorFactory() : Child.CreateExecutorFactory();
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenTheFactoryCallIsDeferredToAnExecutionMethod()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed class DeferredExecutor(ICrossover<int> child, ResolutionScope scope) : CrossoverExecutor<int, SS, P>
            {
                public override IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random, SS searchSpace, P problem) =>
                    Activate().Cross(parents, random, searchSpace, problem);
                private ICrossoverExecutor<int, SS, P> Activate() => child.CreateExecutorFactory<SS, P>()(scope);
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Theory]
    [InlineData("source.CreateExecutorFactory<SS, P>()", 0)]
    [InlineData("Other.CreateExecutorFactory<SS, P>()", 1)]
    public async Task ResolutionPreparation_OnlyAllowsItsOwnSource(string preparation, int count)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file static class CustomResolution
            {
                public static ICrossoverExecutor<int, SS, P> Resolve(ResolutionScope scope, ICrossover<int> configuration, ICrossover<int> Other) =>
                    scope.Resolve(configuration, source => {{preparation}},
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
                public static ICrossoverExecutor<int, SS, P> Resolve(ResolutionScope scope, ICrossover<int> configuration) =>
                    scope.Resolve<ICrossover<int>, ICrossoverExecutor<int, SS, P>>(configuration, source => innerScope => source.CreateExecutorFactory<SS, P>()(innerScope),
                        static (_, _) => throw new System.InvalidOperationException());
            }
            """);
        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenWrapperPreparationConstructsAChildFactoryDirectly()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record CustomWrapper(ICrossover<int> Child) : WrappingCrossover<int>(Child)
            {
                protected override WrapperExecutorFactory<ICrossoverExecutor<int, TSS, TP>> CreateWrapperFactory<TSS, TP>()
                {
                    var childFactory = Child.CreateExecutorFactory<TSS, TP>();
                    return inner => inner;
                }
            }
            """);
        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_ForAConsumerDefinedExecutionContract()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file interface ICustomExecutor : IExecutor;
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
            }
            file sealed class DeliberateCaller
            {
                public int Build(ResolutionScope scope) => new UnrelatedChild().CreateExecutorFactory()(scope);
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("CreateOperatorFactory", "OperatorExecutorFactory")]
    [InlineData("CreateFactory", "ExecutorFactory<IMutationExecutor<int, SS, P>>")]
    public async Task Reports_WhenGenericOperatorPreparationBypassesTheChildScope(string hook, string resultType)
    {
        var body = hook == "CreateOperatorFactory"
            ? "Child.CreateExecutorFactory()"
            : "scope => (IMutationExecutor<int, SS, P>)Child.CreateExecutorFactory().CreateExecutor<Mutation, int, SS, P, HEAL.HeuristicLib.Algorithms.ISearchState>(scope.For<int, SS, P, HEAL.HeuristicLib.Algorithms.ISearchState>())";
        var baseType = hook == "CreateOperatorFactory" ? "Operator<Mutation, int>" : "HEAL.HeuristicLib.Operators.Mutators.Mutator<int>";
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record GenericOperator : {{baseType}}
            {
                public required IOperator<Mutation, int> Child { get; init; }
                protected override {{resultType}} {{hook}}() => {{body}};
            }
            """);
        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenAnOperatorFactoryConstructsItsChildWithoutResolvingIt()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed class CompositionFactory(IOperator<Mutation, int> child) : OperatorExecutorFactory<Mutation, int>
            {
                protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                    ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
                    child.CreateExecutorFactory().CreateExecutor<Mutation, int, TSearchSpace, TProblem, TSearchState>(scope);
            }
            """);
        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenASameTypeChildBypassesResolutionThroughTheProtectedHook()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record RecursiveLeaf : HEAL.HeuristicLib.Operators.Mutators.Mutator<int>
            {
                public RecursiveLeaf? Child { get; init; }
                protected override ExecutorFactory<IMutationExecutor<int, SS, P>> CreateFactory() =>
                    Child is null ? _ => throw new System.InvalidOperationException() : Child.CreateFactory();
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
