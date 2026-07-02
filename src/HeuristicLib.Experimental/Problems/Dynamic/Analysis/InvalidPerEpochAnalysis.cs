using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Analysis;

public class InvalidPerEpochAnalysis<TCandidate>(IDynamicProblem<TCandidate, ISearchSpace<TCandidate>> problem) : DynamicAnalysis<TCandidate>(problem)
{

    private readonly Dictionary<int, int> invalidPerEpoch = [];

    public IReadOnlyDictionary<int, int> InvalidPerEpoch => invalidPerEpoch;
    protected override void Problem_OnEvaluation(object? sender, IReadOnlyList<(TCandidate, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog)
    {
        foreach (var e in evaluationLog.Where(x => !x.timing.Valid))
        {
            var epoch = e.timing.Epoch;
            invalidPerEpoch[epoch] = invalidPerEpoch.GetValueOrDefault(epoch, 0) + 1;
        }
    }
}
