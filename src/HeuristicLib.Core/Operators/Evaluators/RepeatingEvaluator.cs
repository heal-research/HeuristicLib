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
//   public double PredictAndTrain(TGenotype Solution) {
//     return problem.PredictAndTrain(Solution);
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
//   public double PredictAndTrain(TGenotype Solution) {
//     return problem.PredictAndTrain(Solution, random);
//   }
// }

public class RepeatingEvaluator<TGenotype, TSearchSpace, TProblem> : BatchEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator;
  private readonly IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator;
  private readonly int repeats;

  public RepeatingEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator)
  {
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(repeats, 0);
    this.evaluator = evaluator;
    this.repeats = repeats;
    this.aggregator = aggregator;
  }

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem)
  {
    ObjectiveVector[]? results = null;

    for (var i = 0; i < repeats; i++) {
      var singleResult = evaluator.Evaluate(genotypes, random, encoding, problem);
      if (results is null) {
        results = singleResult.ToArray();
      } else {
        for (var j = 0; j < results.Length; j++) {
          results[j] = aggregator(results[j], singleResult[j]);
        }
      }
    }

    return results!;
  }
}
