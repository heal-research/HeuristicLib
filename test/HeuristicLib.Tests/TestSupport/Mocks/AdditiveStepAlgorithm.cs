using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public sealed record AdditiveStepAlgorithm(int Increment)
    : Algorithm<AdditiveStepAlgorithm, int, PopulationState<int>>
{
    public IEvaluator<int> Evaluator { get; init; } = new ProblemEvaluator<int>();

    public override ExecutionFactory<IAlgorithmExecution<int, TRunSearchSpace, TRunProblem, PopulationState<int>>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>() =>
        scope => new Execution<TRunSearchSpace, TRunProblem>(scope.Resolve<int, TRunSearchSpace, TRunProblem>(Evaluator), Increment);

    private sealed class Execution<TSearchSpace, TProblem>(IEvaluatorExecution<int, TSearchSpace, TProblem> evaluator, int increment)
        : AlgorithmExecution<int, TSearchSpace, TProblem, PopulationState<int>>
        where TSearchSpace : class, ISearchSpace<int>
        where TProblem : class, IProblem<int, TSearchSpace>
    {
        private readonly IEvaluatorExecution<int, TSearchSpace, TProblem> evaluator = evaluator;

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
