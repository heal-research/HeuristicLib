using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.Dynamic.Analysis;

public abstract class DynamicAnalysis<TGenotype, TEncoding, TProblem>
  where TGenotype : class
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : IDynamicProblem<TGenotype, TEncoding> {
  protected readonly TProblem Problem;

  protected DynamicAnalysis(TProblem problem) {
    problem.OnEvaluation += Problem_OnEvaluation;
    Problem = problem;
  }

  protected abstract void Problem_OnEvaluation(object? sender, IReadOnlyList<(TGenotype, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog);
}

public abstract class DynamicAnalysis<TGenotype, TEncoding>(IDynamicProblem<TGenotype, TEncoding> problem) :
  DynamicAnalysis<TGenotype, TEncoding, IDynamicProblem<TGenotype, TEncoding>>(problem)
  where TGenotype : class
  where TEncoding : class, IEncoding<TGenotype>;

public abstract class DynamicAnalysis<TGenotype>(IDynamicProblem<TGenotype, IEncoding<TGenotype>> problem) :
  DynamicAnalysis<TGenotype, IEncoding<TGenotype>, IDynamicProblem<TGenotype, IEncoding<TGenotype>>>(problem)
  where TGenotype : class;

public class QualityCurvePerEpochAnalysis<TGenotype>(IDynamicProblem<TGenotype, IEncoding<TGenotype>> problem) :
  DynamicAnalysis<TGenotype>(problem)
  where TGenotype : class {
  private readonly List<(TGenotype solution, ObjectiveVector objectiveVector, EvaluationTiming timing)> bestPerEpoch = [];
  public IReadOnlyList<(TGenotype solution, ObjectiveVector objectiveVector, EvaluationTiming timing)> BestPerEpoch => bestPerEpoch;

  protected override void Problem_OnEvaluation(object? sender, IReadOnlyList<(TGenotype, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog) {
    foreach (var e in evaluationLog.Where(x => x.timing.Valid)) {
      if (bestPerEpoch.Count > 0) {
        var best = bestPerEpoch[^1];
        if (best.timing.Epoch == e.timing.Epoch && Problem.Objective.TotalOrderComparer.Compare(best.objectiveVector, e.objective) >= 0)
          continue;
      }

      bestPerEpoch.Add(e);
    }
  }
}
