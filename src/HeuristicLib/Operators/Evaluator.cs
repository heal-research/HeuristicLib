using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<TGenotype> : IOperator {
  Phenotype<TGenotype>[] Evaluate(TGenotype[] population);
}

public abstract class EvaluatorBase<TGenotype> : IEvaluator<TGenotype> {
 public abstract Phenotype<TGenotype>[] Evaluate(TGenotype[] population);
}

public static class Evaluator {
  public static IEvaluator<TGenotype> UsingFitnessFunction<TGenotype>(Func<TGenotype, Fitness> evaluator) => new FitnessFunctionEvaluator<TGenotype>(evaluator);
}

public abstract class FitnessFunctionEvaluatorBase<TGenotype> : EvaluatorBase<TGenotype> {
  // Define the "runner" (sequential, parallel, ...)
  public abstract Fitness Evaluate(TGenotype individual);
  public override Phenotype<TGenotype>[] Evaluate(TGenotype[] population) {
    return population
      .Select(individual => new Phenotype<TGenotype>(individual, Evaluate(individual)))
      .ToArray();
  }
}

public class FitnessFunctionEvaluator<TGenotype> : FitnessFunctionEvaluatorBase<TGenotype> {
  private readonly Func<TGenotype, Fitness> fitnessFunction;
  public FitnessFunctionEvaluator(Func<TGenotype, Fitness> fitnessFunction) {
    this.fitnessFunction = fitnessFunction;
  }
  public override Fitness Evaluate(TGenotype individual) => fitnessFunction(individual);
}
