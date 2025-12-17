using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

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

public class RepeatingEvaluator<TGenotype, TEncoding, TProblem> : BatchEvaluator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  private readonly IEvaluator<TGenotype, TEncoding, TProblem> evaluator;
  private readonly int repeats;
  private readonly Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator;

  public RepeatingEvaluator(IEvaluator<TGenotype, TEncoding, TProblem> evaluator, int repeats, Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator) {
    this.evaluator = evaluator;
    this.repeats = repeats;
    this.aggregator = aggregator;
  }

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    ObjectiveVector[]? results = null;

    for (var i = 0; i < repeats; i++) {
      var singleResult = evaluator.Evaluate(solutions, random, encoding, problem);
      if (results is null) {
        results = singleResult.ToArray();
      } else {
        for (var j = 0; j < results.Length; j++) {
          results[j] = aggregator(results[j], singleResult[j]);
        }
      }
    }

    return results;
  }
}
