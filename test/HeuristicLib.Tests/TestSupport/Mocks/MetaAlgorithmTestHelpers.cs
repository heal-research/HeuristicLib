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

    public override ExecutorFactory<IEvaluationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> CreateExecutorFactory()
    {
        ExecutionCount++;
        var executor = new Executor();
        return _ => executor;
    }

    private sealed class Executor : EvaluationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            problem.Evaluate(candidates, random).ToArray();
    }
}

public sealed record CountingExecutionAlgorithm(int Increment, IEvaluator<int> Evaluator)
    : Algorithm<CountingExecutionAlgorithm, int, PopulationState<int>>
{
    public int ExecutionCount { get; private set; }

    public override ExecutorFactory<IAlgorithmExecutor<int, TRunSearchSpace, TRunProblem, PopulationState<int>>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
    {
        ExecutionCount++;
        return scope => new Executor<TRunSearchSpace, TRunProblem>(Increment, scope.Resolve<int, TRunSearchSpace, TRunProblem>(Evaluator));
    }

    private sealed class Executor<TSearchSpace, TProblem>(int increment, IEvaluationExecutor<int, TSearchSpace, TProblem> evaluator)
        : AlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>
        where TSearchSpace : class, ISearchSpace<int>
        where TProblem : class, IProblem<int, TSearchSpace>
    {
        public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var current = initialState?.Population.EvaluatedCandidates.Single().Candidate ?? 0;
            var next = current + increment;
            var objectiveVector = evaluator.Evaluate([next], random, problem.SearchSpace, problem).Single();

            yield return Population.From([next.ToEvaluated(objectiveVector)]).ToPopulationState();
            await Task.CompletedTask;
        }
    }
}
