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
    public async Task OperatorConfiguration_AllowsWithExpressionsOfItsOwnType()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ValidMutator(int Offset) : StatelessMutator<int>
          {
              public static System.Func<ValidMutator, int, ValidMutator> WithOffset { get; } = static (mutator, value) => mutator with { Offset = value };

              public ValidMutator Shifted => this with { Offset = Offset + 1 };

              public override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  IRandomNumberGenerator random) => parents;
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConfigurationMutationRule_CoversAssignmentsInArrayInitializers()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : StatelessMutator<int>
          {
              private int calls;

              public override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  IRandomNumberGenerator random)
              {
                  var values = new[] { calls = 7 };
                  return parents;
              }
          }
          """);

        diagnostics.ShouldHaveSingleItem().GetMessage().ShouldContain("calls");
    }

    [Fact]
    public async Task ConfigurationMutationRule_CoversNestedMemberInitializers()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record InvalidMutator : StatelessMutator<int>
          {
              private sealed class Holder(InvalidMutator current)
              {
                  public InvalidMutator Current { get; } = current;
              }

              public int Value { get; set; }

              public override IReadOnlyList<int> Mutate(
                  IReadOnlyList<int> parents,
                  IRandomNumberGenerator random)
              {
                  var holder = new Holder(this) { Current = { Value = 7 } };
                  return parents;
              }
          }
          """);

        diagnostics.ShouldHaveSingleItem().GetMessage().ShouldContain("Value");
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

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        var platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            ?? [];

        var projectAssemblies = new[]
        {
            typeof(Mutator<>).Assembly.Location,
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

        return [.. analyzerDiagnostics.Where(static diagnostic => diagnostic.Id == OperatorAuthoringAnalyzer.ConfigurationMutationDiagnosticId)];
    }
}
