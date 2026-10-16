using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Operators.Mutators;
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
        using HEAL.HeuristicLib.Operators.Mutators;
        using HEAL.HeuristicLib.Problems;
        using HEAL.HeuristicLib.Random;
        using HEAL.HeuristicLib.SearchSpaces;
        using SS = HEAL.HeuristicLib.SearchSpaces.ISearchSpace<int>;
        using P = HEAL.HeuristicLib.Problems.IProblem<int, HEAL.HeuristicLib.SearchSpaces.ISearchSpace<int>>;

        file record ChildMutator : Mutator<int>
        {
            public override ExecutorFactory<IMutationExecutor<int, SS, P>> CreateExecutorFactory() => _ => new Executor();
            private sealed class Executor : MutationExecutor<int, SS, P>
            {
                public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => parents;
            }
        }

        """;

    [Theory]
    [InlineData("IMutator<int>", "Child.CreateExecutorFactory<SS, P>()(scope)")]
    [InlineData("ChildMutator", "Child.CreateExecutorFactory()(scope)")]
    public async Task Reports_WhenAChildFactoryIsInvokedInsideTheBinding(string childType, string binding)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record BypassingMutator : Mutator<int>
            {
                public required {{childType}} Child { get; init; }
                public override ExecutorFactory<IMutationExecutor<int, SS, P>> CreateExecutorFactory() => scope => {{binding}};
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Theory]
    [InlineData("Child.CreateExecutorFactory<SS, P>()")]
    [InlineData("Prepare(); ExecutorFactory<IMutationExecutor<int, SS, P>> Prepare() => Child.CreateExecutorFactory<SS, P>()")]
    public async Task Reports_WhenAChildFactoryIsPreparedDirectly(string preparation)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record BypassingMutator : Mutator<int>
            {
                public required IMutator<int> Child { get; init; }
                public override ExecutorFactory<IMutationExecutor<int, SS, P>> CreateExecutorFactory()
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
            file sealed record BypassingMutator : Mutator<int>
            {
                public required IMutator<int> Child { get; init; }
                public override ExecutorFactory<IMutationExecutor<int, SS, P>> CreateExecutorFactory()
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
            file sealed record ResolvingMutator : Mutator<int>
            {
                public required IMutator<int> Child { get; init; }
                public override ExecutorFactory<IMutationExecutor<int, SS, P>> CreateExecutorFactory() => scope => scope.Resolve<int, SS, P>(Child);
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
                public static IMutationExecutor<int, SS, P> Create(ChildMutator mutator) => mutator.CreateExecutorFactory()(ResolutionScope.Create());
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
            file sealed record DelegatingMutator : ChildMutator
            {
                public override ExecutorFactory<IMutationExecutor<int, SS, P>> CreateExecutorFactory() => {{preparation}};
                private ExecutorFactory<IMutationExecutor<int, SS, P>> CreateExecutorFactory(int unused) => base.CreateExecutorFactory();
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reports_WhenTheBypassIsInsideAnExplicitInterfaceImplementation()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record BridgingMutator : IMutator<int>
            {
                public required ChildMutator Child { get; init; }
                ExecutorFactory<IMutationExecutor<int, TRunSearchSpace, TRunProblem>> IMutator<int>.CreateExecutorFactory<TRunSearchSpace, TRunProblem>() =>
                    scope => (IMutationExecutor<int, TRunSearchSpace, TRunProblem>)Child.CreateExecutorFactory()(scope);
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DoesNotReport_WhenAnExplicitBridgeCallsItsOwnHook()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record BridgingMutator : ChildMutator, IMutator<int>
            {
                ExecutorFactory<IMutationExecutor<int, TRunSearchSpace, TRunProblem>> IMutator<int>.CreateExecutorFactory<TRunSearchSpace, TRunProblem>() =>
                    (ExecutorFactory<IMutationExecutor<int, TRunSearchSpace, TRunProblem>>)CreateExecutorFactory();
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Reports_WhenTheChildSharesItsHolderType()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record RecursiveMutator : ChildMutator
            {
                public RecursiveMutator? Child { get; init; }
                public override ExecutorFactory<IMutationExecutor<int, SS, P>> CreateExecutorFactory() =>
                    Child is null ? base.CreateExecutorFactory() : Child.CreateExecutorFactory();
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenTheFactoryCallIsDeferredToAnExecutionMethod()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed class DeferredExecutor(IMutator<int> child, ResolutionScope scope) : MutationExecutor<int, SS, P>
            {
                public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) =>
                    Activate().Mutate(parents, random, searchSpace, problem);
                private IMutationExecutor<int, SS, P> Activate() => child.CreateExecutorFactory<SS, P>()(scope);
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
                public static IMutationExecutor<int, SS, P> Resolve(ResolutionScope scope, IMutator<int> configuration, IMutator<int> Other) =>
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
                public static IMutationExecutor<int, SS, P> Resolve(ResolutionScope scope, IMutator<int> configuration) =>
                    scope.Resolve<IMutator<int>, IMutationExecutor<int, SS, P>>(configuration, source => innerScope => source.CreateExecutorFactory<SS, P>()(innerScope),
                        static (_, _) => throw new System.InvalidOperationException());
            }
            """);
        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutorFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenWrapperPreparationConstructsAChildFactoryDirectly()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record CustomWrapper(IMutator<int> Child) : WrappingMutator<int>(Child)
            {
                protected override WrapperExecutorFactory<IMutationExecutor<int, TSS, TP>> CreateWrapperFactory<TSS, TP>()
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

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        var platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            ?? [];

        var compilation = CSharpCompilation.Create(
            assemblyName: "ExecutionFactoryAnalyzerTest",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))],
            references: platformAssemblies.Append(MetadataReference.CreateFromFile(typeof(Mutator<>).Assembly.Location)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        var diagnostics = await compilation.WithAnalyzers([new ExecutorFactoryAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        diagnostics.ShouldNotContain(static diagnostic => diagnostic.Id == "AD0001");
        return [.. diagnostics.Where(static diagnostic => diagnostic.Id == ExecutorFactoryAnalyzer.DiagnosticId)];
    }
}
