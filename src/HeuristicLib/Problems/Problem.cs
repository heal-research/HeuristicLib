namespace HEAL.HeuristicLib.Problems;


public interface IProblem<TSolution>
{
  double Evaluate(TSolution solution);
}

public abstract class ProblemBase<TSolution> : IProblem<TSolution>
{
  public abstract double Evaluate(TSolution solution);
}
