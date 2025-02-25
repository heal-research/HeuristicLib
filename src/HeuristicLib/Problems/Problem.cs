using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem<TPhenotype>
{
  IEvaluator<TPhenotype> CreateEvaluator();
  double Evaluate(TPhenotype solution);
}

public abstract class ProblemBase<TPhenotype> : IProblem<TPhenotype>
{
  public abstract IEvaluator<TPhenotype> CreateEvaluator();
  public abstract double Evaluate(TPhenotype solution);
}
