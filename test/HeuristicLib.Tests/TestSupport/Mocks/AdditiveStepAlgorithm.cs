using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public sealed record AdditiveStepAlgorithm(int Increment)
    : Algorithm<AdditiveStepAlgorithm, int, PopulationState<int>>
{
    public IOperator<Evaluation, int> Evaluator { get; init; } = new ProblemEvaluator<int>();

    public static Parameter<AdditiveStepAlgorithm, IOperator<Evaluation, int>> EvaluatorParameter { get; } =
        new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), EvaluatorParameter];

    protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
        new Executor(scope.Resolve(Evaluator), Increment);

    private sealed class Executor(IEvaluationExecutor<int> evaluator, int increment)
        : AlgorithmExecutor<PopulationState<int>>
    {
        private readonly IEvaluationExecutor<int> evaluator = evaluator;

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
