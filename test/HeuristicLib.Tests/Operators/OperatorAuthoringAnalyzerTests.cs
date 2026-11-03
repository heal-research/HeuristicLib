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
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.ResourceGraphDiagnosticId);
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

    [Fact]
    public async Task ResourceHandlesRejectExecutionGraphMembers()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed class InvalidResource
          {
              public IExecutor Executor { get; init; } = null!;
          }
          file sealed class InvalidHandle : ResourceHandle<InvalidResource>
          {
              protected override InvalidResource CreateResource() => new();
          }
          """);
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == OperatorAuthoringAnalyzer.ResourceGraphDiagnosticId);
    }

    [Theory]
    [InlineData("ResolutionScope")]
    [InlineData("RunResources")]
    [InlineData("AlgorithmRun")]
    public async Task ResourceLookupRejectsAnExecutorAsTheResource(string lookupType)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
          file static class ResourceConsumer
          {
              public static IExecutor Lookup({{lookupType}} resources, ResourceHandle<IExecutor> handle) => resources.Resource(handle);
          }
          """);
        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.ResourceGraphDiagnosticId);
        diagnostic.GetMessage().ShouldContain("'IExecutor'");
    }

    [Theory]
    [InlineData("ResolutionScope")]
    [InlineData("RunResources")]
    [InlineData("AlgorithmRun")]
    public async Task ResourceLookupRejectsExecutionGraphMembers(string lookupType)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
          file sealed class InvalidResource
          {
              public IExecutor Executor { get; init; } = null!;
          }
          file static class ResourceConsumer
          {
              public static InvalidResource Lookup({{lookupType}} resources, ResourceHandle<InvalidResource> handle) => resources.Resource(handle);
          }
          """);
        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Id.ShouldBe(OperatorAuthoringAnalyzer.ResourceGraphDiagnosticId);
        diagnostic.GetMessage().ShouldContain("'Executor'");
        diagnostic.GetMessage().ShouldContain("'IExecutor'");
    }

    [Theory]
    [InlineData("ResolutionScope")]
    [InlineData("RunResources")]
    [InlineData("AlgorithmRun")]
    public async Task ResourceLookupAllowsOrdinaryManagedData(string lookupType)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
          file sealed class Tally { public int Count { get; set; } }
          file static class ResourceConsumer
          {
              public static Tally Lookup({{lookupType}} resources, ResourceHandle<Tally> handle) => resources.Resource(handle);
          }
          """);
        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResourcesAllowOrdinaryManagedData()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed class Tally { public int Count { get; set; } }
          file sealed class TallyHandle : ResourceHandle<Tally>
          {
              protected override Tally CreateResource() => new();
          }
          """);
        diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("private IExecutor child = null!;")]
    [InlineData("public List<IConfigurationNode> Children { get; } = new();")]
    [InlineData("private Holder data = new(); private sealed class Holder { public ResolutionScope Scope { get; init; } = null!; }")]
    public async Task ResourceGraphRuleFindsPrivateAndTransitiveDependencies(string members)
    {
        var diagnostics = await AnalyzeAsync(Preamble + $$"""
          file sealed class InvalidResource { {{members}} }
          file sealed class InvalidHandle : ResourceHandle<InvalidResource>
          {
              protected override InvalidResource CreateResource() => new();
          }
          """);
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == OperatorAuthoringAnalyzer.ResourceGraphDiagnosticId);
    }

    [Fact]
    public async Task HandlesCannotRetainAResolutionContext()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed class InvalidHandle : ResourceHandle<object>
          {
              private ResolutionScope scope = ResolutionScope.Create();
              protected override object CreateResource() => new();
          }
          """);
        diagnostics.ShouldContain(diagnostic => diagnostic.Id == OperatorAuthoringAnalyzer.ResourceGraphDiagnosticId);
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

        return [.. analyzerDiagnostics.Where(static diagnostic => diagnostic.Id is OperatorAuthoringAnalyzer.ResourceGraphDiagnosticId or OperatorAuthoringAnalyzer.ConfigurationMutationDiagnosticId)];
    }
}
