using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.Dynamic.Analysis;

public class InvalidPerEpochAnalysis<TGenotype>(IDynamicProblem<TGenotype, IEncoding<TGenotype>> problem) : DynamicAnalysis<TGenotype>(problem) where TGenotype : class {
  protected override void Problem_OnEvaluation(object? sender, IReadOnlyList<(TGenotype, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog) {
    foreach (var e in evaluationLog.Where(x => !x.timing.Valid)) {
      var epoch = e.timing.Epoch;
      invalidPerEpoch[epoch] = invalidPerEpoch.GetValueOrDefault(epoch, 0) + 1;
    }
  }

  private readonly Dictionary<int, int> invalidPerEpoch = [];

  public IReadOnlyDictionary<int, int> InvalidPerEpoch => invalidPerEpoch;
}
