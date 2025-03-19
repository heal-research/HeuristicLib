using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<TGenotype, TFitness> : IOperator {
  Phenotype<TGenotype, TFitness>[] Evaluate(TGenotype[] population);
}

public static class Evaluator {
  public static IEvaluator<TGenotype, TFitness> UsingFitnessFunction<TGenotype, TFitness>(Func<TGenotype, TFitness> evaluator) =>
    new FitnessFunctionEvaluator<TGenotype, TFitness>(evaluator);
}

public abstract class FitnessFunctionEvaluatorBase<TGenotype, TFitness> : IEvaluator<TGenotype, TFitness> {
  // Define the "runner" (sequential, parallel, ...)
  protected FitnessFunctionEvaluatorBase() { }
  public abstract TFitness Evaluate(TGenotype individual);
  public Phenotype<TGenotype, TFitness>[] Evaluate(TGenotype[] population) {
    return population
      .Select(individual => new Phenotype<TGenotype, TFitness>(individual, Evaluate(individual)))
      .ToArray();
  }
}

public class FitnessFunctionEvaluator<TGenotype, TFitness> : FitnessFunctionEvaluatorBase<TGenotype, TFitness> {
  private readonly Func<TGenotype, TFitness> fitnessFunction;
  public FitnessFunctionEvaluator(Func<TGenotype, TFitness> fitnessFunction) {
    this.fitnessFunction = fitnessFunction;
  }
  public override TFitness Evaluate(TGenotype individual) => fitnessFunction(individual);
}
