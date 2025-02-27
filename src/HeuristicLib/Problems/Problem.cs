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
  public static GeneticAlgorithmBuilderBasic<TSolution> WithProblemDefinition<TSolution>(this GeneticAlgorithmBuilderBasic<TSolution> builderBasic, IProblem<TSolution, ObjectiveValue> problem)
  {
    //builder.WithEncodingBundle(problem.GetDefaultEncoding())
    builderBasic.WithEvaluator(problem.CreateEvaluator());
    return builderBasic;
  }
}
