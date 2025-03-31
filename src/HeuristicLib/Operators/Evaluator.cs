using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

// ToDo: class for individual "FitnessFunction"

public interface IEvaluator<in TPhenotype> : IOperator {
  Fitness Evaluate(TPhenotype phenotype);
}

public abstract class EvaluatorBase<TPhenotype> : IEvaluator<TPhenotype> {
 public abstract Fitness Evaluate(TPhenotype phenotype);
}

public static class Evaluator {
  public static CustomEvaluator<TPhenotype> FromFitnessFunction<TPhenotype>(Func<TPhenotype, Fitness> evaluator) {
    return new CustomEvaluator<TPhenotype>(evaluator);
  }
  public static ProblemEvaluator<TPhenotype> FromProblem<TPhenotype>(IProblem<TPhenotype> problem) {
    return new ProblemEvaluator<TPhenotype>(problem);
  }
}

public class CustomEvaluator<TPhenotype> : EvaluatorBase<TPhenotype> {
  private readonly Func<TPhenotype, Fitness> fitnessFunction;
  public CustomEvaluator(Func<TPhenotype, Fitness> fitnessFunction) {
    this.fitnessFunction = fitnessFunction;
  }
  public override Fitness Evaluate(TPhenotype phenotype) => fitnessFunction(phenotype);
}

public class ProblemEvaluator<TPhenotype> : EvaluatorBase<TPhenotype> {
  private readonly IProblem<TPhenotype> problem;

  public ProblemEvaluator(IProblem<TPhenotype> problem) {
    this.problem = problem;
  }
  public override Fitness Evaluate(TPhenotype phenotype) => problem.Evaluate(phenotype);
}
