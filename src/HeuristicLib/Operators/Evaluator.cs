namespace HEAL.HeuristicLib.Operators;

// ToDo: class for individual "FitnessFunction"

public interface IEvaluator<in TPhenotype/*, out TEvaluationResult*/> {
  /*TEvaluationResult*/Fitness Evaluate(TPhenotype phenotype);
}

public interface IFitnessExtractor<in TEvaluationResult> {
  Fitness Extract(TEvaluationResult evaluationResult);
}

public abstract class EvaluatorBase<TPhenotype> : IEvaluator<TPhenotype> {
 public abstract Fitness Evaluate(TPhenotype phenotype);
}

public static class Evaluator {
  public static FitnessFunctionEvaluator<TPhenotype> FromFitnessFunction<TPhenotype>(Func<TPhenotype, Fitness> evaluator) {
    return new FitnessFunctionEvaluator<TPhenotype>(evaluator);
  }
  // public static ProblemEvaluator<TPhenotype> FromProblem<TPhenotype>(IProblem<TPhenotype> problem) {
  //   return new ProblemEvaluator<TPhenotype>(problem);
  // }
}

public class FitnessFunctionEvaluator<TPhenotype> : EvaluatorBase<TPhenotype> {
  private readonly Func<TPhenotype, Fitness> fitnessFunction;
  public FitnessFunctionEvaluator(Func<TPhenotype, Fitness> fitnessFunction) {
    this.fitnessFunction = fitnessFunction;
  }
  public override Fitness Evaluate(TPhenotype phenotype) => fitnessFunction(phenotype);
}

// public class ProblemEvaluator<TPhenotype> : EvaluatorBase<TPhenotype> {
//   private readonly IProblem<TPhenotype> problem;
//
//   public ProblemEvaluator(IProblem<TPhenotype> problem) {
//     this.problem = problem;
//   }
//   public override Fitness Evaluate(TPhenotype phenotype) => problem.Evaluate(phenotype);
// }
