using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Operators;
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
      using HEAL.HeuristicLib.Operators.Mutators;
      using HEAL.HeuristicLib.Problems;
      using HEAL.HeuristicLib.Random;
      using HEAL.HeuristicLib.SearchSpaces;

      """;

    [Fact]
    public async Task StatefulMutator_AllowsOrdinaryExecutionData()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ValidMutator : StatefulMutator<int, ValidMutator.State>
          {
              public sealed class State
              {
                  public int Calls { get; set; }
                  public Dictionary<int, int> Cache { get; } = new();
              }

              protected override State CreateInitialState() => new();

              protected override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  State state,
                  IRandomNumberGenerator random)
              {
                  state.Calls++;
                  return parents;
              }
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task StatefulMutator_RejectsExecutionInstanceMember()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : StatefulMutator<int, InvalidMutator.State>
          {
              public sealed class State
              {
                  public IExecutionInstance? ChildInstance { get; set; }
              }

              protected override State CreateInitialState() => new();

              protected override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  State state,
                  IRandomNumberGenerator random) => parents;
          }
          """);

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.StatefulStateDiagnosticId);
        diagnostic.GetMessage().ShouldContain("ChildInstance");
    }

    [Fact]
    public async Task StatefulMutator_RejectsOperatorHiddenInGenericContainer()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : StatefulMutator<int, InvalidMutator.State>
          {
              public sealed class State
              {
                  public List<IMutator<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>> Children
                      { get; } = new();
              }

              protected override State CreateInitialState() => new();

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
                  public ExecutionInstanceRegistry? Registry { get; set; }
              }

              protected override State CreateInitialState() => new();

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
                  public IExecutionInstance[] Instances { get; set; } = [];
                  public (int Count, IExecutionInstance? Instance) Snapshot { get; set; }
              }

              protected override State CreateInitialState() => new();

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
          file abstract record CustomStatefulOperator<TState> : IOperator<CustomOperatorInstance>
              where TState : class
          {
              protected abstract TState CreateInitialState();

              public CustomOperatorInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => new();
          }

          file sealed class CustomOperatorInstance : IOperatorInstance;

          file sealed record InvalidOperator : CustomStatefulOperator<InvalidOperator.State>
          {
              public sealed class State
              {
                  public ExecutionInstanceRegistry? Registry { get; set; }
              }

              protected override State CreateInitialState() => new();
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

              protected override State CreateInitialState() => new();

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
          file sealed record InvalidOperator : IOperator<InvalidOperator>, IOperatorInstance
          {
              private int calls;

              public InvalidOperator CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;

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
              protected override IProblem<int, ISearchSpace<int>> CreateInitialState() => null!;

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
              protected override ISearchSpace<int> CreateInitialState() => null!;

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

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        var platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            ?? [];

        var projectAssemblies = new[]
        {
            typeof(StatefulMutator<,>).Assembly.Location,
            typeof(IMutator<,,>).Assembly.Location
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
