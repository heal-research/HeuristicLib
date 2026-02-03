using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public interface IDynamicProblem<TGenotype, out TEncoding> : IProblem<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  event EventHandler<IReadOnlyList<(TGenotype, ObjectiveVector, EvaluationTiming)>>? OnEvaluation;
}
