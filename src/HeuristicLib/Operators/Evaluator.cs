namespace HEAL.HeuristicLib.Operators;




public interface IEvaluator<TSolution>
{
  double Evaluate(TSolution solution);
}
