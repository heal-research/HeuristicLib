using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Operators.Mutators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HEAL.HeuristicLib.Tests.Operators;

/// <summary>Compile real factory contracts before checking that child preparation cannot silently bypass resolution.</summary>
public class ExecutionFactoryAnalyzerTests
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
            public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory() => _ => new Execution();
            private sealed class Execution : MutatorExecution<int, SS, P>
            {
                public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => parents;
            }
        }

        """;

    [Theory]
    [InlineData("IMutator<int>", "Child.CreateExecutionFactory<SS, P>()(scope)")]
    [InlineData("ChildMutator", "Child.CreateExecutionFactory()(scope)")]
    public async Task Reports_WhenAChildFactoryIsInvokedInsideTheBinding(string childType, string binding)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record BypassingMutator : Mutator<int>
            {
                public required {{childType}} Child { get; init; }
                public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory() => scope => {{binding}};
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutionFactoryAnalyzer.DiagnosticId);
    }

    [Theory]
    [InlineData("Child.CreateExecutionFactory<SS, P>()")]
    [InlineData("Prepare(); ExecutionFactory<IMutatorExecution<int, SS, P>> Prepare() => Child.CreateExecutionFactory<SS, P>()")]
    public async Task Reports_WhenAChildFactoryIsPreparedDirectly(string preparation)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record BypassingMutator : Mutator<int>
            {
                public required IMutator<int> Child { get; init; }
                public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory()
                {
                    return {{preparation}};
                }
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutionFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenAChildFactoryIsStoredDuringPreparation()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record BypassingMutator : Mutator<int>
            {
                public required IMutator<int> Child { get; init; }
                public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory()
                {
                    var factory = Child.CreateExecutionFactory<SS, P>();
                    return scope => factory(scope);
                }
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutionFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DoesNotReport_WhenTheChildIsResolvedThroughTheScope()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record ResolvingMutator : Mutator<int>
            {
                public required IMutator<int> Child { get; init; }
                public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory() => scope => scope.Resolve<int, SS, P>(Child);
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
                public static IMutatorExecution<int, SS, P> Create(ChildMutator mutator) => mutator.CreateExecutionFactory()(ResolutionScope.Create());
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("CreateExecutionFactory(1)")]
    [InlineData("this.CreateExecutionFactory(1)")]
    [InlineData("base.CreateExecutionFactory()")]
    public async Task DoesNotReport_WhenAFactoryHookDelegatesToItselfOrItsBase(string preparation)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record DelegatingMutator : ChildMutator
            {
                public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory() => {{preparation}};
                private ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory(int unused) => base.CreateExecutionFactory();
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
                ExecutionFactory<IMutatorExecution<int, TRunSearchSpace, TRunProblem>> IMutator<int>.CreateExecutionFactory<TRunSearchSpace, TRunProblem>() =>
                    scope => (IMutatorExecution<int, TRunSearchSpace, TRunProblem>)Child.CreateExecutionFactory()(scope);
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutionFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DoesNotReport_WhenAnExplicitBridgeCallsItsOwnHook()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed record BridgingMutator : ChildMutator, IMutator<int>
            {
                ExecutionFactory<IMutatorExecution<int, TRunSearchSpace, TRunProblem>> IMutator<int>.CreateExecutionFactory<TRunSearchSpace, TRunProblem>() =>
                    (ExecutionFactory<IMutatorExecution<int, TRunSearchSpace, TRunProblem>>)CreateExecutionFactory();
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
                public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory() =>
                    Child is null ? base.CreateExecutionFactory() : Child.CreateExecutionFactory();
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutionFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Reports_WhenTheFactoryCallIsDeferredToAnExecutionMethod()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed class DeferredExecution(IMutator<int> child, ResolutionScope scope) : MutatorExecution<int, SS, P>
            {
                public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) =>
                    Activate().Mutate(parents, random, searchSpace, problem);
                private IMutatorExecution<int, SS, P> Activate() => child.CreateExecutionFactory<SS, P>()(scope);
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutionFactoryAnalyzer.DiagnosticId);
    }

    [Theory]
    [InlineData("scope.Resolve(Child, child => child.CreateExecutionFactory<SS, P>())", 0)]
    [InlineData("scope.Resolve(Child, child => Other.CreateExecutionFactory<SS, P>())", 1)]
    public async Task PreparationAdapter_OnlyAllowsItsOwnSource(string resolution, int count)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
            file sealed record ResolvingMutator : Mutator<int>
            {
                public required IMutator<int> Child { get; init; }
                public required IMutator<int> Other { get; init; }
                public override ExecutionFactory<IMutatorExecution<int, SS, P>> CreateExecutionFactory() => scope => {{resolution}};
            }
            """);

        diagnostics.Length.ShouldBe(count);
    }

    [Fact]
    public async Task Reports_ForAConsumerDefinedExecutionContract()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file interface ICustomExecution : IExecutionNode;
            file sealed record CustomConfiguration : IConfigurationNode<ICustomExecution>
            {
                public required IConfigurationNode<ICustomExecution> Child { get; init; }
                public ExecutionFactory<ICustomExecution> CreateExecutionFactory() => scope => Child.CreateExecutionFactory()(scope);
            }
            """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(ExecutionFactoryAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DoesNotReport_UnrelatedMethodsWithMatchingNames()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
            file sealed class UnrelatedChild
            {
                public System.Func<ResolutionScope, int> CreateExecutionFactory() => _ => 0;
            }
            file sealed class DeliberateCaller
            {
                public int Build(ResolutionScope scope) => new UnrelatedChild().CreateExecutionFactory()(scope);
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
        var diagnostics = await compilation.WithAnalyzers([new ExecutionFactoryAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        diagnostics.ShouldNotContain(static diagnostic => diagnostic.Id == "AD0001");
        return [.. diagnostics.Where(static diagnostic => diagnostic.Id == ExecutionFactoryAnalyzer.DiagnosticId)];
    }
}
