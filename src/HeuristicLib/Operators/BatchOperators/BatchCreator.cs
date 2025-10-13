using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.BatchOperators;

public abstract class BatchCreator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}
