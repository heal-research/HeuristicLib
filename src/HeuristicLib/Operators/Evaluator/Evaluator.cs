using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public abstract class Evaluator<TGenotype, TEncoding, TProblem> : IEvaluator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding> 
{
  public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, TEncoding, TProblem>.Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    return solutions.ParallelSelect(random, (_, x, r) => Evaluate(x, r, encoding, problem));
  }
}

public abstract class Evaluator<TGenotype, TEncoding> : IEvaluator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TEncoding encoding) {
    return parents.ParallelSelect(random, (_, x, r) => Evaluate(x, r, encoding));
  }

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Evaluate(IReadOnlyList<TGenotype> parents, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Evaluate(parents, random, encoding);
  }
}
