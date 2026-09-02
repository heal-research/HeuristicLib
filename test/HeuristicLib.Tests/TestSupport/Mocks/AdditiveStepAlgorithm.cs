using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public sealed record AdditiveStepAlgorithm(int Increment)
    : Algorithm<AdditiveStepAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
{
    public IEvaluator<int> Evaluator { get; init; } = new ProblemEvaluator<int>();

    public override AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(instanceRegistry.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(Evaluator), Increment);

    private sealed class Instance(IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator, int increment)
        : AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        private readonly IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator = evaluator;

        public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IProblem<int, DummySearchSpace<int>> problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
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
