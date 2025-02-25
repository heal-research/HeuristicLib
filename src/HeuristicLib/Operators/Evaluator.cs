namespace HEAL.HeuristicLib.Operators;




public interface IEvaluator<in TPhenotype, out TObjective>
{
  TObjective Evaluate(TPhenotype solution);
}
