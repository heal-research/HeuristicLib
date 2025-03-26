using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IEvaluatorOperator<TGenotype> : IExecutableOperator {
  Phenotype<TGenotype>[] Evaluate(TGenotype[] population);
}

public abstract class EvaluatorOperatorBase<TGenotype> : IEvaluatorOperator<TGenotype> {
 public abstract Phenotype<TGenotype>[] Evaluate(TGenotype[] population);
}

public static class EvaluatorOperator {
  public static FitnessFunctionEvaluatorOperator<TGenotype> UsingFitnessFunction<TGenotype>(Func<TGenotype, Fitness> evaluator) => new FitnessFunctionEvaluatorOperator<TGenotype>(evaluator);
}

public abstract class FitnessFunctionEvaluatorOperatorBase<TGenotype> : EvaluatorOperatorBase<TGenotype> {
  // Define the "runner" (sequential, parallel, ...)
  public abstract Fitness Evaluate(TGenotype individual);
  public override Phenotype<TGenotype>[] Evaluate(TGenotype[] population) {
    return population
      .Select(individual => new Phenotype<TGenotype>(individual, Evaluate(individual)))
      .ToArray();
  }
}

public class FitnessFunctionEvaluatorOperator<TGenotype> : FitnessFunctionEvaluatorOperatorBase<TGenotype> {
  private readonly Func<TGenotype, Fitness> fitnessFunction;
  public FitnessFunctionEvaluatorOperator(Func<TGenotype, Fitness> fitnessFunction) {
    this.fitnessFunction = fitnessFunction;
  }
  public override Fitness Evaluate(TGenotype individual) => fitnessFunction(individual);
}
