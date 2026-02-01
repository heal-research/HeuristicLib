using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract class SingleSolutionEvaluator<TGenotype, TSearchSpace, TProblem> : Evaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => genotypes.ParallelSelect(random, (_, x, r) => Evaluate(x, r, searchSpace, problem));
}

public abstract class SingleSolutionEvaluator<TGenotype, TSearchSpace> : Evaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace) => genotypes.ParallelSelect(random, (_, x, r) => Evaluate(x, r, searchSpace));

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Evaluate(genotypes, random, searchSpace);
}
