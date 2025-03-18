using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem { }

public interface IProblem<in TGenotype, out TFitness, out TGoal> : IProblem {
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
