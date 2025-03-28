using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

// ToDo: class for individual "FitnessFunction"

public interface IEvaluator<in TPhenotype> : IOperator {
  Fitness[] Evaluate(TPhenotype[] population);
}

public abstract class EvaluatorBase<TPhenotype> : IEvaluator<TPhenotype> {
 public abstract Fitness[] Evaluate(TPhenotype[] population);
}

public static class Evaluator {
  public static CustomFitnessFunctionEvaluator<TPhenotype> FromFitnessFunction<TPhenotype>(Func<TPhenotype, Fitness> evaluator) {
    return new CustomFitnessFunctionEvaluator<TPhenotype>(evaluator);
  }
  public static ProblemFitnessFunctionEvaluator<TPhenotype> FromProblem<TPhenotype>(IProblem<TPhenotype> problem) {
    return new ProblemFitnessFunctionEvaluator<TPhenotype>(problem);
  }
}

public abstract class FitnessFunctionEvaluatorBase<TPhenotype> : EvaluatorBase<TPhenotype> {
  // Define the "runner" (sequential, parallel, ...)
  protected FitnessFunctionEvaluatorBase() {}
  public abstract Fitness Evaluate(TPhenotype phenotype);
  public override Fitness[] Evaluate(TPhenotype[] population) {
    return population.Select(Evaluate).ToArray();
  }
}

public class CustomFitnessFunctionEvaluator<TPhenotype> : FitnessFunctionEvaluatorBase<TPhenotype> {
  private readonly Func<TPhenotype, Fitness> fitnessFunction;
  public CustomFitnessFunctionEvaluator(Func<TPhenotype, Fitness> fitnessFunction) {
    this.fitnessFunction = fitnessFunction;
  }
  public override Fitness Evaluate(TPhenotype phenotype) => fitnessFunction(phenotype);
}

public class ProblemFitnessFunctionEvaluator<TPhenotype> : FitnessFunctionEvaluatorBase<TPhenotype> {
  private readonly IProblem<TPhenotype> problem;

  public ProblemFitnessFunctionEvaluator(IProblem<TPhenotype> problem) {
    this.problem = problem;
  }
  public override Fitness Evaluate(TPhenotype phenotype) => problem.Evaluate(phenotype);
}
