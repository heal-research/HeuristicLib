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

//
// public static class GeneticAlgorithmBuilderProblemExtension {
//   public static GeneticAlgorithmBuilder<TEncoding, TGenotype> WithProblemDefinition<TEncoding, TGenotype>(this GeneticAlgorithmBuilder<TEncoding, TGenotype> builder, IProblem<TGenotype, ObjectiveValue> problem)
//     where TEncoding : IEncoding<TGenotype>
//   {
//     //builder.WithEncodingBundle(problem.GetDefaultEncoding())
//     builder.WithEvaluator(problem.CreateEvaluator());
//     return builder;
//   }
// }
