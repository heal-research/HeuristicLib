using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public class AlgorithmInterfaceCapabilityTests
{
    [Theory]
    [InlineData("held.Complete(problem, rng)")]
    [InlineData("held.CompleteAsync(problem, rng)")]
    [InlineData("held.Stream(problem, rng)")]
    [InlineData("held.CreateRun(problem, rng)")]
    [InlineData("held.Validate(problem)")]
    [InlineData("held.TerminatedAfterIterations(10)")]
    [InlineData("held.TerminatedBy(new NeverTerminator<Permutation>())")]
    [InlineData("held.LimitedToDuration(TimeSpan.FromSeconds(1))")]
    [InlineData("held.ObserveWith((PopulationState<Permutation> state) => { })")]
    [InlineData("held.AsGrid()")]
    [InlineData("held.Repeat(2)")]
    [InlineData("held.CycleWith(algorithm)")]
    [InlineData("held.Then(algorithm)")]
    public void ExecutionSurface_IsReachableOnlyWhenTheAlgorithmNamesItsSearchState(string expression)
    {
        // Both halves matter: the first proves the expression itself is well formed, so the second is failing over
        // the interface rather than over a typo in the probe.
        Compiles("IAlgorithm<Permutation, PopulationState<Permutation>>", expression).ShouldBeTrue();
        Compiles("IAlgorithm<Permutation>", expression).ShouldBeFalse();
    }

    [Theory]
    [InlineData("new List<IAlgorithm<Permutation>> { algorithm }")]
    [InlineData("new ExecutionInstanceRegistry().Resolve<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, PopulationState<Permutation>>(held)")]
    public void ErasedSurface_IsReachableFromBothForms(string expression)
    {
        Compiles("IAlgorithm<Permutation, PopulationState<Permutation>>", expression).ShouldBeTrue();
        Compiles("IAlgorithm<Permutation>", expression).ShouldBeTrue();
    }

    private const string ProbeTemplate = """
        using System;
        using System.Collections.Generic;
        using HEAL.HeuristicLib.Algorithms;
        using HEAL.HeuristicLib.Encodings.Permutations;
        using HEAL.HeuristicLib.Execution;
        using HEAL.HeuristicLib.Experiments;
        using HEAL.HeuristicLib.Operators;
        using HEAL.HeuristicLib.Problems.TravelingSalesman;
        using HEAL.HeuristicLib.Random;

        public static class CapabilityProbe
        {
            public static void Use(GeneticAlgorithm<Permutation> algorithm, TravelingSalesmanProblem problem)
            {
                {HOLDING} held = algorithm;
                var rng = RandomNumberGenerator.Create(0);
                _ = {EXPRESSION};
            }
        }
        """;

    private static bool Compiles(string holding, string expression)
    {
        var source = ProbeTemplate.Replace("{HOLDING}", holding).Replace("{EXPRESSION}", expression);

        var compilation = CSharpCompilation.Create(
            "AlgorithmCapabilityProbe",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return !compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);
    }

    private static readonly ImmutableArray<MetadataReference> References = BuildReferences();

    private static ImmutableArray<MetadataReference> BuildReferences()
    {
        var platformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var libraries = new[] { typeof(IAlgorithm<>).Assembly, typeof(GeneticAlgorithm<>).Assembly }
            .Select(assembly => assembly.Location);

        return
        [
            .. platformAssemblies.Concat(libraries)
                                 .Where(location => !string.IsNullOrEmpty(location))
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .Select(location => (MetadataReference)MetadataReference.CreateFromFile(location))
        ];
    }
}
