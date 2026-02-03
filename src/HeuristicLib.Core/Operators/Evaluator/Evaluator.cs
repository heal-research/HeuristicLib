using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public abstract class Evaluator<TGenotype, TEncoding, TProblem> : IEvaluator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding> {
  protected abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    return genotypes.ParallelSelect(random, (_, x, r) => Evaluate(x, r, encoding, problem));
  }
}

public abstract class Evaluator<TGenotype, TEncoding> : IEvaluator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TEncoding encoding) {
    return genotypes.ParallelSelect(random, (_, x, r) => Evaluate(x, r, encoding));
  }

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Evaluate(genotypes, random, encoding);
  }
}
