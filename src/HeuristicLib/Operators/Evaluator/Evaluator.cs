using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public abstract class Evaluator<TGenotype, TSearchSpace, TProblem> : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace> {
  protected abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) {
    return genotypes.ParallelSelect(random, (_, x, r) => Evaluate(x, r, searchSpace, problem));
  }
}

public abstract class Evaluator<TGenotype, TSearchSpace> : IEvaluator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace) {
    return genotypes.ParallelSelect(random, (_, x, r) => Evaluate(x, r, searchSpace));
  }

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return Evaluate(genotypes, random, searchSpace);
  }
}
