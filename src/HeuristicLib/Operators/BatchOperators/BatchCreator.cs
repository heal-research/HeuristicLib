using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.BatchOperators;

public abstract class BatchCreator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchCreator<TGenotype> : ICreator<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Create(count, random);
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>>.Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Create(count, random);
  }
}

public abstract class BatchCreator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Create(count, random, encoding);
  }
}
