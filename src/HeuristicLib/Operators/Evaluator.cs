namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<in TPhenotype, out TObjective> : IOperator {
  TObjective Evaluate(TPhenotype solution);
}

public static class Evaluator {
  public static IEvaluator<TPhenotype, TObjectice> Create<TPhenotype, TObjectice>(Func<TPhenotype, TObjectice> evaluator) => new Evaluator<TPhenotype, TObjectice>(evaluator);
}

public sealed class Evaluator<TPhenotype, TObjective> : IEvaluator<TPhenotype, TObjective> {
  private readonly Func<TPhenotype, TObjective> evaluator;
  internal Evaluator(Func<TPhenotype, TObjective> evaluator) {
    this.evaluator = evaluator;
  }
  public TObjective Evaluate(TPhenotype solution) => evaluator(solution);
}

public abstract class EvaluatorBase<TPhenotype, TObjective> : IEvaluator<TPhenotype, TObjective> {
  public abstract TObjective Evaluate(TPhenotype solution);
}
