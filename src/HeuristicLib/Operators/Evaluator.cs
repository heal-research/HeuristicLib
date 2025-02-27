namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<in TPhenotype, out TObjective> : IOperator {
  TObjective Evaluate(TPhenotype solution);
}

public interface IEvaluatorTemplate<out TEvaluator, TPhenotype, TObjective, in TParams>
  : IOperatorTemplate<TEvaluator, TParams>
  where TEvaluator : IEvaluator<TPhenotype, TObjective> {
}

public record EvaluatorParameters();

public abstract class EvaluatorBase<TPhenotype, TObjective> : IEvaluator<TPhenotype, TObjective> {
  public abstract TObjective Evaluate(TPhenotype solution);
}

public abstract class EvaluatorTemplateBase<TEvaluator, TGenotype, TObjective, TParams>
  : IEvaluatorTemplate<TEvaluator, TGenotype, TObjective, TParams>
  where TEvaluator : IEvaluator<TGenotype, TObjective>
  where TParams : EvaluatorParameters {
  public abstract TEvaluator Parameterize(TParams parameters);
}
