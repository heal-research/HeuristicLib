using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public sealed record AdditiveStepAlgorithm(int Increment)
    : Algorithm<AdditiveStepAlgorithm, int, PopulationState<int>>
{
    public IOperator<Evaluation, int> Evaluator { get; init; } = new ProblemEvaluator<int>();

    protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
        new Executor<TSearchSpace, TProblem>(scope.Resolve(Evaluator), Increment);

    private sealed class Executor<TSearchSpace, TProblem>(IEvaluationExecutor<int, TSearchSpace, TProblem> evaluator, int increment)
        : AlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>
        where TSearchSpace : class, ISearchSpace<int>
        where TProblem : class, IProblem<int, TSearchSpace>
    {
        private readonly IEvaluationExecutor<int, TSearchSpace, TProblem> evaluator = evaluator;

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
