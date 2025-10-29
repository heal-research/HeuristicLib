using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public abstract class BatchCrossover<TGenotype> : ICrossover<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Cross(parents, random);
  }

  IReadOnlyList<TGenotype> ICrossover<TGenotype, IEncoding<TGenotype>>.Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Cross(parents, random);
  }
}

public abstract class BatchCrossover<TGenotype, TEncoding> : ICrossover<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<TGenotype> ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Cross(parents, random, encoding);
  }
}
