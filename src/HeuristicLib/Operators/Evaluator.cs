namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<in TGenotype, out TFitness> : IOperator {
  TFitness Evaluate(TGenotype solution);
}

public static class Evaluator {
  public static IEvaluator<TGenotype, TFitness> Create<TGenotype, TFitness>(Func<TGenotype, TFitness> evaluator) => new Evaluator<TGenotype, TFitness>(evaluator);
}

public sealed class Evaluator<TGenotype, TFitness> : IEvaluator<TGenotype, TFitness> {
  private readonly Func<TGenotype, TFitness> evaluator;
  internal Evaluator(Func<TGenotype, TFitness> evaluator) {
    this.evaluator = evaluator;
  }
  public TFitness Evaluate(TGenotype solution) => evaluator(solution);
}

public abstract class EvaluatorBase<TGenotype, TFitness> : IEvaluator<TGenotype, TFitness> {
  public abstract TFitness Evaluate(TGenotype solution);
}
