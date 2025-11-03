using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using MoreLinq;

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

public class RepeatingEvaluator<TGenotype, TEncoding, TProblem>(IEvaluator<TGenotype, TEncoding, TProblem> evaluator, int count, Func<IEnumerable<ObjectiveVector>, ObjectiveVector> aggregator) : IEvaluator<TGenotype, TEncoding, TProblem> where TEncoding : class, IEncoding<TGenotype> where TProblem : IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TEncoding encoding, TProblem problem) {
    var tests = genotypes.Repeat(count).ToArray();
    var subResults = evaluator.Evaluate(tests, encoding, problem); //avoid calling evaluator multiple times
    return Enumerable.Range(0, genotypes.Count)
                     .Select(localI => aggregator(Enumerable.Range(0, count).Select(j => subResults[j * genotypes.Count + localI])))
                     .ToArray();
  }
}
