using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;

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
    public int InstanceCount { get; private set; }

    protected override IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateEvaluatorInstance(ExecutionInstanceRegistry registry)
    {
        InstanceCount++;
        return new Instance();
    }

    private sealed class Instance : EvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            problem.Evaluate(candidates, random);
    }
}

public sealed record CountingInstanceAlgorithm(int Increment, IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> Evaluator)
    : Algorithm<CountingInstanceAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
{
    public int InstanceCount { get; private set; }

    protected override AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateAlgorithmInstance(ExecutionInstanceRegistry registry)
    {
        InstanceCount++;
        return new Instance(Increment, registry.Resolve(Evaluator));
    }

    private sealed class Instance(int increment, IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator)
        : AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IProblem<int, DummySearchSpace<int>> problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var current = initialState?.Population.EvaluatedCandidates.Single().Candidate ?? 0;
            var next = current + increment;
            var objective = evaluator.Evaluate([next], random, problem.SearchSpace, problem).Single();

            yield return Population.From([EvaluatedCandidate.From(next, objective)]).ToPopulationState();
            await Task.CompletedTask;
        }
    }
}
