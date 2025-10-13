namespace HEAL.HeuristicLib.Operators;

// public class DeterministicProblemEvaluator<TGenotype> : IEvaluator<TGenotype>
// {
//   private readonly IDeterministicProblem<TGenotype> problem;
//   public DeterministicProblemEvaluator(IDeterministicProblem<TGenotype> problem) {
//     this.problem = problem;
//   }
//
//   public double Evaluate(TGenotype solution) {
//     return problem.Evaluate(solution);
//   }
// }
//
// public class StochasticEvaluator<TGenotype> : IEvaluator<TGenotype>
// {
//   private readonly IStochasticProblem<TGenotype> problem;
//   private readonly IRandomNumberGenerator random;
//   
//   public StochasticEvaluator(IStochasticProblem<TGenotype> problem, IRandomNumberGenerator random) {
//     this.problem = problem;
//     this.random = random;
//   }
//
//   public double Evaluate(TGenotype solution) {
//     return problem.Evaluate(solution, random);
//   }
// }

public class RepeatingEvaluator<TGenotype>(IEvaluator<TGenotype> evaluator, int count) : IEvaluator<TGenotype> {
  public double Evaluate(TGenotype solution) {
    return Enumerable.Range(0, count).Select(i => evaluator.Evaluate(solution)).Average();
  }
}
