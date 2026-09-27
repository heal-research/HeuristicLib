using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Operators.Mutators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HEAL.HeuristicLib.Tests.Operators;

/// <summary>
/// HLib0001 is the only mechanical enforcement of resolution going through the scope, and it identifies what it
/// guards by two literal names. A rename would disable it silently: the build reports a false positive loudly, but
/// nothing reports it ceasing to fire. These specs are that missing report.
/// </summary>
public class CreateExecutionInstanceAnalyzerTests
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

      """;

    /// <remarks>
    /// The shape every migrated role actually has: the child is held behind its interface, so the creation method is
    /// generic in the run's search space and problem.
    /// </remarks>
    [Fact]
    public async Task Reports_WhenAChildIsCreatedThroughTheGenericInterfaceMethod()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record BypassingMutator : Mutator<int>
          {
              public required IMutator<int> Child { get; init; }

              public override IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope) =>
                  Child.CreateExecutionInstance<SS, P>(scope);
          }
          """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(CreateExecutionInstanceAnalyzer.DiagnosticId);
    }

    /// <remarks>The bound base declares a non-generic creation method, so a child held by its own type offers that one.</remarks>
    [Fact]
    public async Task Reports_WhenAChildIsCreatedThroughTheNonGenericBoundMethod()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ChildMutator : Mutator<int>
          {
              public override IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope) =>
                  new Execution();

              private sealed class Execution : MutatorExecution<int, SS, P>
              {
                  public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => parents;
              }
          }

          file sealed record BypassingMutator : Mutator<int>
          {
              public required ChildMutator Child { get; init; }

              public override IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope) =>
                  Child.CreateExecutionInstance(scope);
          }
          """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(CreateExecutionInstanceAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task DoesNotReport_WhenTheChildIsResolvedThroughTheRegistry()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ResolvingMutator : Mutator<int>
          {
              public required IMutator<int> Child { get; init; }

              public override IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope) =>
                  scope.Resolve<int, SS, P>(Child);
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    /// <remarks>
    /// The rule is about nesting, not about the call. A caller that is not itself building an execution graph has no
    /// scope to resolve through, so direct creation is the only spelling available to it.
    /// </remarks>
    [Fact]
    public async Task DoesNotReport_WhenTheCallSiteIsNotItselfACreationMethod()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ChildMutator : Mutator<int>
          {
              public override IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope) =>
                  new Execution();

              private sealed class Execution : MutatorExecution<int, SS, P>
              {
                  public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => parents;
              }
          }

          file static class DeliberateCaller
          {
              public static IMutatorExecution<int, SS, P> Create(ChildMutator mutator) =>
                  mutator.CreateExecutionInstance(ResolutionScope.Create());
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task DoesNotReport_WhenACreationMethodCallsItsOwnOverload()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record DelegatingMutator : Mutator<int>
          {
              public override IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope) =>
                  CreateExecutionInstance(scope, 1);

              private IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope, int unused) =>
                  new Execution();

              private sealed class Execution : MutatorExecution<int, SS, P>
              {
                  public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => parents;
              }
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    /// <remarks>
    /// Every role base writes its bridge as an explicit interface implementation, so a consumer writing a composite
    /// the same way is the case the rule most needs to reach.
    /// </remarks>
    [Fact]
    public async Task Reports_WhenTheBypassIsInsideAnExplicitInterfaceImplementation()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record ChildMutator : Mutator<int>
          {
              public override IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope) =>
                  new Execution();

              private sealed class Execution : MutatorExecution<int, SS, P>
              {
                  public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => parents;
              }
          }

          file sealed record BridgingMutator : IMutator<int>
          {
              public required ChildMutator Child { get; init; }

              IMutatorExecution<int, TRunSearchSpace, TRunProblem> IMutator<int>.CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope) =>
                  (IMutatorExecution<int, TRunSearchSpace, TRunProblem>)Child.CreateExecutionInstance(scope);
          }
          """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(CreateExecutionInstanceAnalyzer.DiagnosticId);
    }

    /// <remarks>
    /// A derived operator asking its base for an instance has no other spelling — <c>scope.Resolve(this)</c> would
    /// resolve back to itself.
    /// </remarks>
    [Fact]
    public async Task DoesNotReport_WhenACreationMethodCallsItsBase()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file abstract record BaseMutator : Mutator<int>
          {
              public override IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope) =>
                  new Execution();

              private sealed class Execution : MutatorExecution<int, SS, P>
              {
                  public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => parents;
              }
          }

          file sealed record DerivedMutator : BaseMutator
          {
              public override IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope) =>
                  base.CreateExecutionInstance(scope);
          }
          """);

        diagnostics.ShouldBeEmpty();
    }

    /// <remarks>Sharing a type with its holder does not make a child a self call; it is still owned, so it is resolved.</remarks>
    [Fact]
    public async Task Reports_WhenTheChildSharesItsHolderType()
    {
        var diagnostics = await AnalyzeAsync(Preamble + """
          file sealed record RecursiveMutator : Mutator<int>
          {
              public RecursiveMutator? Child { get; init; }

              public override IMutatorExecution<int, SS, P> CreateExecutionInstance(ResolutionScope scope) =>
                  Child is null ? new Execution() : Child.CreateExecutionInstance(scope);

              private sealed class Execution : MutatorExecution<int, SS, P>
              {
                  public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, SS searchSpace, P problem) => parents;
              }
          }
          """);

        diagnostics.ShouldHaveSingleItem().Id.ShouldBe(CreateExecutionInstanceAnalyzer.DiagnosticId);
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
            typeof(IMutator<>).Assembly.Location
        }.Distinct().Select(static path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            assemblyName: "CreateExecutionInstanceAnalyzerTest",
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
            .WithAnalyzers([new CreateExecutionInstanceAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();

        return [.. analyzerDiagnostics.Where(static diagnostic => diagnostic.Id == CreateExecutionInstanceAnalyzer.DiagnosticId)];
    }
}
