namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<in TGentype> {
  double Evaluate(TGentype solution);
}
