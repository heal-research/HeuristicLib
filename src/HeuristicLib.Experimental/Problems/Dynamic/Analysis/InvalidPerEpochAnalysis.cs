using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed record InvalidPerEpochAnalysis<TCandidate, TSearchSpace, TProblem>
    : DynamicAnalysis<TCandidate, TSearchSpace, TProblem, InvalidPerEpochAnalysisResult<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
{
    public InvalidPerEpochAnalysis(TProblem problem, params IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators)
        : base(problem, evaluators)
    { }

    public override InvalidPerEpochAnalysisResult<TCandidate> CreateInitialResult() => new();
}

public sealed class InvalidPerEpochAnalysisResult<TCandidate> : IDynamicAnalysisResult<TCandidate>
{
    private readonly Dictionary<int, int> invalidPerEpoch = [];

    public IReadOnlyDictionary<int, int> InvalidPerEpoch => invalidPerEpoch;

    public void AfterEvaluationLog(object? sender, IReadOnlyList<(TCandidate candidate, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog)
    {
        foreach (var e in evaluationLog.Where(x => !x.timing.Valid))
        {
            var epoch = e.timing.Epoch;
            invalidPerEpoch[epoch] = invalidPerEpoch.GetValueOrDefault(epoch, 0) + 1;
        }
    }
}
