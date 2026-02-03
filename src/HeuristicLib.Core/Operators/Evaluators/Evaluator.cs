using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract class Evaluator<TGenotype, TSearchSpace, TProblem> : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
{

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem) => genotypes.ParallelSelect(random, action: (_, x, r) => Evaluate(x, r, encoding, problem));
  protected abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);
}

public abstract class Evaluator<TGenotype, TSearchSpace> : IEvaluator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace encoding) => genotypes.ParallelSelect(random, action: (_, x, r) => Evaluate(x, r, encoding));

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace encoding, IProblem<TGenotype, TSearchSpace> problem) => Evaluate(genotypes, random, encoding);
  public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TSearchSpace encoding);
}
