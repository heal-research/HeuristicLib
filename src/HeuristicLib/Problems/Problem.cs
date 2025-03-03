using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem<in TPhenotype, out TObjective>
{
  IEvaluator<TPhenotype, TObjective> CreateEvaluator();
  TObjective Evaluate(TPhenotype solution);
}

public abstract class ProblemBase<TPhenotype, TObjective> : IProblem<TPhenotype, TObjective>
{
  public abstract IEvaluator<TPhenotype, TObjective> CreateEvaluator();
  public abstract TObjective Evaluate(TPhenotype solution);
}


public static class GeneticAlgorithmBuilderProblemExtension {
  public static GeneticAlgorithmBuilder<TSolution> WithProblemDefinition<TSolution>(this GeneticAlgorithmBuilder<TSolution> builder, IProblem<TSolution, ObjectiveValue> problem)
  {
    //builder.WithEncodingBundle(problem.GetDefaultEncoding())
    builder.WithEvaluator(problem.CreateEvaluator());
    return builder;
  }
}
