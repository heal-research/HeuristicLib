using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<in TGenotype> {
  ObjectiveDirection Direction { get; }
  double Evaluate(TGenotype solution);
}
