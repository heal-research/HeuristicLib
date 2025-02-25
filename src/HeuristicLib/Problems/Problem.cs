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
