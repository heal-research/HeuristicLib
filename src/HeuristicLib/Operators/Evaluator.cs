namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<in TPhenotype, out TObjective> : IOperator {
  TObjective Evaluate(TPhenotype solution);
}

public abstract class EvaluatorBase<TPhenotype, TObjective> : IEvaluator<TPhenotype, TObjective> {
  public abstract TObjective Evaluate(TPhenotype solution);
}
