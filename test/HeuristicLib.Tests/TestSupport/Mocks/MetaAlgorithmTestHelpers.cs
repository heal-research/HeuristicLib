using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public static class MetaAlgorithmTestHelpers
{
    public static IProblem<int, DummySearchSpace<int>> CreateIntegerProblem()
    {
        return FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
    }

    public static int StateCandidate(PopulationState<int> state) => state.Population.EvaluatedCandidates.Single().Candidate;

    public static double StateObjective(PopulationState<int> state) => state.Population.EvaluatedCandidates.Single().ObjectiveVector[0];
}

public sealed record CountingResolutionEvaluator : Evaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
{
    public int ExecutionCount { get; private set; }

    // Counts private data construction for each executor.
    protected override IEvaluationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TRunSearchState> scope)
    {
        ExecutionCount++;
        return new Executor(scope.Problem);
    }

    private sealed class Executor(IProblem<int, DummySearchSpace<int>> problem) : EvaluationExecutor<int>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
            problem.Evaluate(candidates, random).ToArray();
    }

}

public sealed record CountingExecutionAlgorithm(int Increment, IOperator<Evaluation, int> Evaluator)
    : Algorithm<CountingExecutionAlgorithm, int, PopulationState<int>>
{
    public int ExecutionCount { get; private set; }

    public static Parameter<CountingExecutionAlgorithm, IOperator<Evaluation, int>> EvaluatorParameter { get; } =
        new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), EvaluatorParameter];

    // Counts private data construction for each executor.
    protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
    {
        ExecutionCount++;
        return new Executor(Increment, scope.Resolve(Evaluator));
    }

    private sealed class Executor(int increment, IEvaluationExecutor<int> evaluator)
        : AlgorithmExecutor<PopulationState<int>>
    {
        public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var current = initialState?.Population.EvaluatedCandidates.Single().Candidate ?? 0;
            var next = current + increment;
            var objectiveVector = evaluator.Evaluate([next], random).Single();

            yield return Population.From([next.ToEvaluated(objectiveVector)]).ToPopulationState();
            await Task.CompletedTask;
        }
    }

}
