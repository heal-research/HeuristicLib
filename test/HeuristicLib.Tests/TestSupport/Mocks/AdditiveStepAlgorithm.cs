using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public sealed record AdditiveStepAlgorithm(int Increment)
    : Algorithm<AdditiveStepAlgorithm, int, PopulationState<int>>
{
    public IEvaluator<int> Evaluator { get; init; } = new ProblemEvaluator<int>();

    public override IAlgorithmInstance<int, TRunSearchSpace, TRunProblem, PopulationState<int>> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance<TRunSearchSpace, TRunProblem>(instanceRegistry.Resolve<int, TRunSearchSpace, TRunProblem>(Evaluator), Increment);

    private sealed class Instance<TSearchSpace, TProblem>(IEvaluatorInstance<int, TSearchSpace, TProblem> evaluator, int increment)
        : AlgorithmInstance<int, TSearchSpace, TProblem, PopulationState<int>>
        where TSearchSpace : class, ISearchSpace<int>
        where TProblem : class, IProblem<int, TSearchSpace>
    {
        private readonly IEvaluatorInstance<int, TSearchSpace, TProblem> evaluator = evaluator;

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
