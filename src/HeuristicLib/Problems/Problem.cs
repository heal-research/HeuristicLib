using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem<in TGenotype, out TFitness, out TGoal> {
  TGoal Goal { get; } 
  TFitness Evaluate(TGenotype solution);
}

public abstract class ProblemBase<TGenotype, TFitness, TGoal> : IProblem<TGenotype, TFitness, TGoal> {
  public TGoal Goal { get; }
  public abstract IEvaluator<TGenotype, TFitness> CreateEvaluator();
  public abstract TFitness Evaluate(TGenotype solution);
  
  protected ProblemBase(TGoal goal) {
    Goal = goal;
  }
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
