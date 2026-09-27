using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Analysis;

/// <summary>
/// Covers an <see cref="AccumulatingAnalyzer"/> end to end: it installs its own observations and merges every source
/// into the one accumulator it publishes.
/// </summary>
public class AccumulatingAnalyzerTests
{
    private static readonly ObjectiveDirections TwoMinimized =
        new([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], NoTotalOrderComparer.Instance);

    [Fact]
    public void OneAccumulator_MergesEveryObservedSource()
    {
        var first = new SketchEvaluator();
        var second = new SketchEvaluator();
        var analyzer = new ParetoFrontAnalyzer<int, SketchSearchSpace, SketchProblem>(
            TwoMinimized, new ObjectiveVector(100, 100), first, second);
        var scope = Install(analyzer);

        analyzer.Front.Count.ShouldBe(0);
        Evaluate(scope, first, 1, 9);
        Evaluate(scope, second, 3, 7);

        analyzer.Front.Points.Select(evaluated => evaluated.Candidate).ShouldBe([1, 3, 7, 9], ignoreOrder: true);

        // The front keeps accumulating, so a later observation shows up in the same front.
        Evaluate(scope, first, 5);
        analyzer.Front.Count.ShouldBe(5);
    }

    private static ResolutionScope Install(params IAnalyzer[] analyzers) =>
        ResolutionScope.Create(builder =>
        {
            foreach (var analyzer in analyzers)
                analyzer.Install(builder);
        });

    private static void Evaluate(ResolutionScope scope, SketchEvaluator evaluator, params int[] candidates) =>
        scope.Resolve<int, SketchSearchSpace, SketchProblem>(evaluator)
             .Evaluate(candidates, RandomNumberGenerator.Create(1), SketchSearchSpace.Instance, SketchProblem.Instance);

    /// <summary>Scores a candidate against two conflicting objectives, so every candidate stays on the front.</summary>
    private sealed record SketchEvaluator : Evaluator<int, SketchSearchSpace, SketchProblem>
    {
        public override IEvaluatorExecution<int, SketchSearchSpace, SketchProblem> CreateExecutionInstance(ResolutionScope scope) => new Execution();

        private sealed class Execution : IEvaluatorExecution<int, SketchSearchSpace, SketchProblem>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random,
                SketchSearchSpace searchSpace, SketchProblem problem) =>
                [.. candidates.Select(candidate => new ObjectiveVector(candidate, 10 - candidate))];
        }
    }

    private sealed class SketchSearchSpace : ISearchSpace<int>
    {
        public static readonly SketchSearchSpace Instance = new();
        private SketchSearchSpace() { }
        public bool Contains(int candidate) => true;
    }

    private sealed class SketchProblem : IProblem<int, SketchSearchSpace>
    {
        public static SketchProblem Instance { get; } = new();
        public ObjectiveDirections Objective => TwoMinimized;
        public SketchSearchSpace SearchSpace => SketchSearchSpace.Instance;
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
            [.. candidates.Select(candidate => new ObjectiveVector(candidate, 10 - candidate))];
    }
}
