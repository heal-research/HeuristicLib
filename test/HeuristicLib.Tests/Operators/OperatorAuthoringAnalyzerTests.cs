using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Operators.Mutators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HEAL.HeuristicLib.Tests.Operators;

public class OperatorAuthoringAnalyzerTests
{
    private const string Preamble = """
      using System.Collections.Generic;
      using HEAL.HeuristicLib.Execution;
      using HEAL.HeuristicLib.Operators;
      using HEAL.HeuristicLib.Operators.Interceptors;
      using HEAL.HeuristicLib.Operators.Mutators;
      using HEAL.HeuristicLib.Problems;
      using HEAL.HeuristicLib.Random;
      using HEAL.HeuristicLib.SearchSpaces;
      using HEAL.HeuristicLib.Algorithms;

      """;

    [Fact]
    public async Task StatefulMutator_AllowsOrdinaryExecutionData()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ValidMutator : StatefulMutator<int, ValidMutator.State>
          {
              public sealed class State
              {
                  public int Invocations { get; set; }
                  public Dictionary<int, int> Cache { get; } = new();
              }

              protected override State CreateScopedState() => new();

              protected override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  State state,
                  IRandomNumberGenerator random)
              {
                  state.Invocations++;
                  return parents;
              }
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task StatefulMutator_RejectsExecutionMember()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : StatefulMutator<int, InvalidMutator.State>
          {
              public sealed class State
              {
                  public IExecutor? ChildExecution { get; set; }
              }

              protected override State CreateScopedState() => new();

              protected override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  State state,
                  IRandomNumberGenerator random) => parents;
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StatefulStateDiagnosticId);
        diagnostic.GetMessage().ShouldContain("ChildExecution");
    }

    [Fact]
    public async Task StatefulMutator_RejectsOperatorHiddenInGenericContainer()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : StatefulMutator<int, InvalidMutator.State>
          {
              public sealed class State
              {
                  public List<IOperator<Mutation, int>> Children
                      { get; } = new();
              }

              protected override State CreateScopedState() => new();

              protected override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  State state,
                  IRandomNumberGenerator random) => parents;
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StatefulStateDiagnosticId);
        diagnostic.GetMessage().ShouldContain("Children");
    }

    [Fact]
    public async Task StatefulMutator_RejectsExecutionDependencyInNestedStateType()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : StatefulMutator<int, InvalidMutator.State>
          {
              public sealed class State
              {
                  public Helper Data { get; } = new();
              }

              public sealed class Helper
              {
                  public ResolutionScope? Registry { get; set; }
              }

              protected override State CreateScopedState() => new();

              protected override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  State state,
                  IRandomNumberGenerator random) => parents;
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StatefulStateDiagnosticId);
        diagnostic.GetMessage().ShouldContain("Data");
    }

    [Fact]
    public async Task StatefulMutator_RejectsExecutionDependenciesInArraysAndTuples()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : StatefulMutator<int, InvalidMutator.State>
          {
              public sealed class State
              {
                  public IExecutor[] Instances { get; set; } = [];
                  public (int Count, IExecutor? Instance) Snapshot { get; set; }
              }

              protected override State CreateScopedState() => new();

              protected override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  State state,
                  IRandomNumberGenerator random) => parents;
          }
          """);

        diagnostics.Length.ShouldBe(2);
        diagnostics.ShouldAllBe(diagnostic => diagnostic.Id == OperatorAuthoringAnalyzer.StatefulStateDiagnosticId);
        diagnostics.Select(static diagnostic => diagnostic.GetMessage()).ShouldContain(message => message.Contains("Instances", StringComparison.Ordinal));
        diagnostics.Select(static diagnostic => diagnostic.GetMessage()).ShouldContain(message => message.Contains("Snapshot", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StatefulOperatorRule_UsesAuthoringShapeWithoutKnownBase()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file abstract record CustomStatefulOperator<TState> : IOperator
              where TState : class
          {
              protected abstract TState CreateScopedState();

              public CustomOperatorExecutor CreateExecutor(ResolutionScope scope) => new();
          }

          file sealed class CustomOperatorExecutor : IOperatorExecutor;

          file sealed record InvalidOperator : CustomStatefulOperator<InvalidOperator.State>
          {
              public sealed class State
              {
                  public ResolutionScope? Registry { get; set; }
              }

              protected override State CreateScopedState() => new();
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StatefulStateDiagnosticId);
        diagnostic.GetMessage().ShouldContain("Registry");
    }

    [Fact]
    public async Task StatelessMutator_RejectsMutationOfConfigurationMember()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : StatelessMutator<int>
          {
              private int calls;

              public override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  IRandomNumberGenerator random)
              {
                  calls++;
                  return parents;
              }
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.ConfigurationMutationDiagnosticId);
        diagnostic.GetMessage().ShouldContain("calls");
    }

    [Fact]
    public async Task StatefulMutator_RejectsMutationOfConfigurationMember()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : StatefulMutator<int, InvalidMutator.State>
          {
              private int calls;

              public sealed class State;

              protected override State CreateScopedState() => new();

              protected override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  State state,
                  IRandomNumberGenerator random)
              {
                  calls++;
                  return parents;
              }
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.ConfigurationMutationDiagnosticId);
        diagnostic.GetMessage().ShouldContain("calls");
    }

    [Fact]
    public async Task StatelessMutator_AllowsConfigurationInitializedBeforeExecution()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ValidMutator(int Offset) : StatelessMutator<int>
          {
              public override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  IRandomNumberGenerator random) => parents;
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task OperatorConfiguration_AllowsObjectInitializerMemberWrites()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ValidMutator : StatelessMutator<int>
          {
              private sealed class Result
              {
                  public int Value { get; init; }
              }

              public override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  IRandomNumberGenerator random)
              {
                  var result = new Result { Value = 1 };
                  return parents;
              }
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task StatelessOperatorRule_UsesAuthoringShapeWithoutKnownBase()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidOperator : IOperator, IOperatorExecutor
          {
              private int calls;

              public InvalidOperator CreateExecutor(ResolutionScope scope) => this;

              public void Execute()
              {
                  calls++;
              }
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.ConfigurationMutationDiagnosticId);
        diagnostic.GetMessage().ShouldContain("calls");
    }

    [Fact]
    public async Task StatefulMutator_RejectsProblemBoundAsStateTypeArgument()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record TrapMutator : StatefulMutator<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
          {
              protected override IProblem<int, ISearchSpace<int>> CreateScopedState() => null!;

              protected override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  IProblem<int, ISearchSpace<int>> state,
                  IRandomNumberGenerator random,
                  ISearchSpace<int> searchSpace) => parents;
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StateContractDiagnosticId);
        diagnostic.GetMessage().ShouldContain("IProblem");
        diagnostic.GetMessage().ShouldContain("TProblem");
    }

    [Fact]
    public async Task StatefulMutator_RejectsSearchSpaceBoundAsStateTypeArgument()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record TrapMutator : StatefulMutator<int, ISearchSpace<int>>
          {
              protected override ISearchSpace<int> CreateScopedState() => null!;

              protected override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  ISearchSpace<int> state,
                  IRandomNumberGenerator random) => parents;
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StateContractDiagnosticId);
        diagnostic.GetMessage().ShouldContain("ISearchSpace");
    }

    /// <summary>
    /// Interceptor and terminator ladders carry a fifth type parameter, so a search state can land in the trailing
    /// <c>TState</c> slot and still compile.
    /// </summary>
    [Fact]
    public async Task StatefulInterceptor_RejectsSearchStateBoundAsStateTypeArgument()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed class TrapSearchState : ISearchState;

          file sealed record TrapInterceptor : StatefulInterceptor<int, TrapSearchState, TrapSearchState>
          {
              protected override TrapSearchState CreateScopedState() => null!;

              protected override TrapSearchState Transform(
                  TrapSearchState currentState,
                  TrapSearchState? previousState,
                  TrapSearchState operatorState,
                  IRandomNumberGenerator random) => currentState;
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StateContractDiagnosticId);
        diagnostic.GetMessage().ShouldContain("TrapSearchState");
        diagnostic.GetMessage().ShouldContain("TSearchState");
    }

    /// <summary>
    /// An ordinary dedicated state type must remain valid on the same ladder.
    /// </summary>
    [Fact]
    public async Task StatefulInterceptor_AllowsDedicatedStateType()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed class OrdinarySearchState : ISearchState;

          file sealed record ValidInterceptor : StatefulInterceptor<int, OrdinarySearchState, ValidInterceptor.State>
          {
              public sealed class State
              {
                  public int Invocations { get; set; }
              }

              protected override State CreateScopedState() => new();

              protected override OrdinarySearchState Transform(
                  OrdinarySearchState currentState,
                  OrdinarySearchState? previousState,
                  State operatorState,
                  IRandomNumberGenerator random)
              {
                  operatorState.Invocations++;
                  return currentState;
              }
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ScopedStateRequest_RejectsExecutionMember()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : Mutator<int>
          {
              public sealed class State
              {
                  public ResolutionScope? Registry { get; set; }
              }

              protected override IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>> CreateExecutor(ResolutionScope scope) =>
                  new Executor(scope.GetScopedState(this, static () => new State()));

              private sealed class Executor(State state) : IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
              {
                  public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, ISearchSpace<int> searchSpace,
                      IProblem<int, ISearchSpace<int>> problem) => state.Registry is null ? parents : [];
              }
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StatefulStateDiagnosticId);
        diagnostic.GetMessage().ShouldContain("Registry");
    }

    [Fact]
    public async Task ScopedStateRequest_RejectsAnExecutorAsState()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : Mutator<int>
          {
              protected override IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>> CreateExecutor(ResolutionScope scope) =>
                  scope.GetScopedState(this, static () => new Executor());

              private sealed class Executor : IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
              {
                  public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, ISearchSpace<int> searchSpace,
                      IProblem<int, ISearchSpace<int>> problem) => parents;
              }
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StatefulStateDiagnosticId);
        diagnostic.GetMessage().ShouldContain("IExecutor");
    }

    [Fact]
    public async Task ScopedStateRequest_AllowsOrdinaryExecutionData()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ValidMutator : Mutator<int>
          {
              public sealed class State
              {
                  public int Invocations { get; set; }
              }

              protected override IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>> CreateExecutor(ResolutionScope scope) =>
                  new Executor(scope.GetScopedState(this, static () => new State()));

              private sealed class Executor(State state) : IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
              {
                  public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, ISearchSpace<int> searchSpace,
                      IProblem<int, ISearchSpace<int>> problem)
                  {
                      state.Invocations++;
                      return parents;
                  }
              }
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ScopedStateRequest_RejectsAnotherConfiguration()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator(IOperator<Mutation, int> Child) : Mutator<int>
          {
              public sealed class State
              {
                  public int Invocations { get; set; }
              }

              protected override IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>> CreateExecutor(ResolutionScope scope) =>
                  new Executor(scope.GetScopedState(Child, static () => new State()));

              private sealed class Executor(State state) : IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
              {
                  public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, ISearchSpace<int> searchSpace,
                      IProblem<int, ISearchSpace<int>> problem) => state.Invocations == 0 ? parents : [];
              }
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StatefulStateDiagnosticId);
        diagnostic.GetMessage().ShouldContain("'Child'");
    }

    [Fact]
    public async Task ScopedStateRequest_AllowsThisAsNamedOrConvertedArgument()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ValidMutator : Mutator<int>
          {
              public sealed class State
              {
                  public int Invocations { get; set; }
              }

              protected override IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>> CreateExecutor(ResolutionScope scope)
              {
                  scope.GetScopedState((IConfigurationNode)this, static () => new State());
                  return new Executor(scope.GetScopedState(create: static () => new State(), configuration: this));
              }

              private sealed class Executor(State state) : IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
              {
                  public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, ISearchSpace<int> searchSpace,
                      IProblem<int, ISearchSpace<int>> problem) => state.Invocations == 0 ? parents : [];
              }
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConfigurationMutationRule_CoversAlgorithms()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidAlgorithm : Algorithm<InvalidAlgorithm, int, PopulationState<int>>
          {
              public int Builds { get; set; }

              protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
                  ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
              {
                  Builds++;
                  throw new System.NotSupportedException();
              }
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.ConfigurationMutationDiagnosticId);
        diagnostic.GetMessage().ShouldContain("'Builds'");
    }

    [Fact]
    public async Task ConfigurationMutationRule_CoversConsumerDefinedRoles()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file interface ITickExecutor : IExecutor;

          file sealed class InvalidTicker : IConfigurationNode<ITickExecutor>
          {
              private int builds;

              public ITickExecutor CreateExecutor(ResolutionScope scope)
              {
                  builds = builds + 1;
                  throw new System.NotSupportedException();
              }
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.ConfigurationMutationDiagnosticId);
        diagnostic.GetMessage().ShouldContain("'builds'");
    }

    [Theory]
    [InlineData("Child.Ticks")]
    [InlineData("ObservationPoint.Of<int>(Child, nameof(Ticks))")]
    public async Task ObservationPointResolution_RejectsAnotherConfiguration(string point)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
          using HEAL.HeuristicLib.Inspection;

          file sealed class InvalidTicker(InvalidTicker Child) : IConfigurationNode<IObservationPointExecutor<int>>
          {
              public ObservationPoint<int> Ticks => ObservationPoint.Of<int>(this, nameof(Ticks));

              public IObservationPointExecutor<int> CreateExecutor(ResolutionScope scope) => scope.Resolve({{point}});
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StatefulStateDiagnosticId);
        diagnostic.GetMessage().ShouldContain("not declared by this configuration");
    }

    [Theory]
    [InlineData("Ticks")]
    [InlineData("this.Ticks")]
    [InlineData("ObservationPoint.Of<int>(this, nameof(Ticks))")]
    public async Task ObservationPointResolution_AllowsOwnPoints(string point)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
          using HEAL.HeuristicLib.Inspection;

          file sealed record ValidTicker : IConfigurationNode<IObservationPointExecutor<int>>
          {
              public ObservationPoint<int> Ticks => ObservationPoint.Of<int>(this, nameof(Ticks));

              public IObservationPointExecutor<int> CreateExecutor(ResolutionScope scope) => scope.Resolve({{point}});
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ObservationPointResolution_AllowsLocalAliasesAndResolutionOutsideBuilds()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          using HEAL.HeuristicLib.Inspection;

          file sealed record ValidTicker : IConfigurationNode<IObservationPointExecutor<int>>
          {
              public ObservationPoint<int> Ticks => ObservationPoint.Of<int>(this, nameof(Ticks));

              public IObservationPointExecutor<int> CreateExecutor(ResolutionScope scope)
              {
                  var ticks = Ticks;
                  return scope.Resolve(ticks);
              }

              public static IObservationPointExecutor<int> ObserveAnother(ResolutionScope scope, ValidTicker other) =>
                  scope.Resolve(other.Ticks);
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

        var projectAssemblies = new[]
        {
            typeof(StatefulMutator<,>).Assembly.Location,
            typeof(IOperator<,>).Assembly.Location
        }.Distinct().Select(static path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            assemblyName: "OperatorAuthoringAnalyzerTest",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(
                    source,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview))
            ],
            references: platformAssemblies.Concat(projectAssemblies),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty();

        var analyzerDiagnostics = await compilation
            .WithAnalyzers([new OperatorAuthoringAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();

        return [.. analyzerDiagnostics.Where(static diagnostic => diagnostic.Id is OperatorAuthoringAnalyzer.StatefulStateDiagnosticId or OperatorAuthoringAnalyzer.ConfigurationMutationDiagnosticId or OperatorAuthoringAnalyzer.StateContractDiagnosticId)];
    }
}
