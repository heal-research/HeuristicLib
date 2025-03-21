using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<TGenotype, TFitness> : IOperator {
  Phenotype<TGenotype, TFitness>[] Evaluate(TGenotype[] population);
}
public interface ISingleObjectiveEvaluator<TGenotype> : IEvaluator<TGenotype, Fitness>;
public interface IMultiObjectiveEvaluator<TGenotype> : IEvaluator<TGenotype, FitnessVector>;

public static class Evaluator {
  // public static IEvaluator<TGenotype, TFitness> UsingFitnessFunction<TGenotype, TFitness>(Func<TGenotype, TFitness> evaluator) =>
  //   new FitnessFunctionEvaluator<TGenotype, TFitness>(evaluator);
  public static ISingleObjectiveEvaluator<TGenotype> UsingFitnessFunction<TGenotype>(Func<TGenotype, Fitness> evaluator) =>
    new SingleObjectiveFitnessFunctionEvaluator<TGenotype>(evaluator);
  public static IMultiObjectiveEvaluator<TGenotype> UsingFitnessFunction<TGenotype>(Func<TGenotype, FitnessVector> evaluator) =>
    new MultiObjectiveFitnessFunctionEvaluator<TGenotype>(evaluator);
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
public abstract class SingleObjectiveFitnessFunctionEvaluatorBase<TGenotype> : FitnessFunctionEvaluatorBase<TGenotype, Fitness>, ISingleObjectiveEvaluator<TGenotype>;
public abstract class MultiObjectiveFitnessFunctionEvaluatorBase<TGenotype> : FitnessFunctionEvaluatorBase<TGenotype, FitnessVector>, IMultiObjectiveEvaluator<TGenotype>;

// public class FitnessFunctionEvaluator<TGenotype, TFitness> : FitnessFunctionEvaluatorBase<TGenotype, TFitness> {
//   private readonly Func<TGenotype, TFitness> fitnessFunction;
//   public FitnessFunctionEvaluator(Func<TGenotype, TFitness> fitnessFunction) {
//     this.fitnessFunction = fitnessFunction;
//   }
//   public override TFitness Evaluate(TGenotype individual) => fitnessFunction(individual);
// }
public class SingleObjectiveFitnessFunctionEvaluator<TGenotype> : SingleObjectiveFitnessFunctionEvaluatorBase<TGenotype> {
  private readonly Func<TGenotype, Fitness> fitnessFunction;
  public SingleObjectiveFitnessFunctionEvaluator(Func<TGenotype, Fitness> fitnessFunction) {
    this.fitnessFunction = fitnessFunction;
  }
  public override Fitness Evaluate(TGenotype individual) => fitnessFunction(individual);
}
public class MultiObjectiveFitnessFunctionEvaluator<TGenotype> : MultiObjectiveFitnessFunctionEvaluatorBase<TGenotype> {
  private readonly Func<TGenotype, FitnessVector> fitnessFunction;
  public MultiObjectiveFitnessFunctionEvaluator(Func<TGenotype, FitnessVector> fitnessFunction) {
    this.fitnessFunction = fitnessFunction;
  }
  public override FitnessVector Evaluate(TGenotype individual) => fitnessFunction(individual);
}
