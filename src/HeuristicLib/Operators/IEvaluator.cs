using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<in TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding> {
  IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TEncoding encoding, TProblem problem);
}

public interface IEvaluator<TGenotype, in TEncoding> : IEvaluator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> where TEncoding : class, IEncoding<TGenotype> { }

public class Evaluator<TGenotype, TEncoding, TProblem>
  : IEvaluator<TGenotype, TEncoding, TProblem> where TEncoding : class, IEncoding<TGenotype> where TProblem : IProblem<TGenotype, TEncoding> {
  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TEncoding encoding, TProblem problem) {
    var results = new ObjectiveVector[genotypes.Count];
    Parallel.For(0, genotypes.Count, i => results[i] = problem.Evaluate(genotypes[i]));
    return results;
  }
}

public class StochasticEvaluator<TGenotype, TEncoding, TProblem> : IEvaluator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype> where TProblem : IStochasticProblem<TGenotype, TEncoding> {
  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TEncoding encoding, TProblem problem) {
    return genotypes.ParallelSelect(problem.ProblemRandom,
      (_, genotype, randomNumberGenerator) => problem.Evaluate(genotype, randomNumberGenerator));
  }
}
