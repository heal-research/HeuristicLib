using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

// public class DeterministicProblemEvaluator<TGenotype> : IEvaluator<TGenotype>
// {
//   private readonly IDeterministicProblem<TGenotype> problem;
//   public DeterministicProblemEvaluator(IDeterministicProblem<TGenotype> problem) {
//     this.problem = problem;
//   }
//
//   public double Evaluate(TGenotype Solution) {
//     return problem.Evaluate(Solution);
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
//   public double Evaluate(TGenotype Solution) {
//     return problem.Evaluate(Solution, random);
//   }
// }

public class RepeatingEvaluator<TGenotype, TSearchSpace, TProblem> : Evaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  private readonly IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator;
  private readonly int repeats;
  private readonly Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator;

  public RepeatingEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator) {
    this.evaluator = evaluator;
    this.repeats = repeats;
    this.aggregator = aggregator;
  }

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) {
    ObjectiveVector[] results = evaluator.Evaluate(solutions, random, searchSpace, problem).ToArray();

    for (int i = 0; i < repeats; i++) {
      var reevaluationResult = evaluator.Evaluate(solutions, random, searchSpace, problem);
      for (int j = 0; j < results.Length; j++) {
          results[j] = aggregator(results[j], reevaluationResult[j]);
      }
    }

    return results;
  }
}
