using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Creator;

public abstract class Creator<TGenotype, TEncoding, TProblem> : ICreator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract TGenotype Create(IRandomNumberGenerator random, TEncoding encoding, TProblem problem);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Create(r, encoding, problem)).ToArray();
  }
}

public abstract class Creator<TGenotype, TEncoding> : ICreator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract TGenotype Create(IRandomNumberGenerator random, TEncoding encoding);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Create(r, encoding)).ToArray();
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Create(int count, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Create(count, random, encoding);
  }
}

public abstract class Creator<TGenotype> : ICreator<TGenotype> {
  public abstract TGenotype Create(IRandomNumberGenerator random);

  public IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random) {
    return Enumerable.Range(0, count).ParallelSelect(random, (_, _, r) => Create(r)).ToArray();
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Create(count, random);
  }

  IReadOnlyList<TGenotype> ICreator<TGenotype, IEncoding<TGenotype>>.Create(int count, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Create(count, random);
  }
}
