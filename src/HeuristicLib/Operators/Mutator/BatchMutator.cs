using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutator;

public abstract class BatchMutator<TGenotype, TEncoding, TProblem> : IMutator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}

public abstract class BatchMutator<TGenotype> : IMutator<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return Mutate(parent, random);
  }

  IReadOnlyList<TGenotype> IMutator<TGenotype, IEncoding<TGenotype>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, IEncoding<TGenotype> encoding) {
    return Mutate(parent, random);
  }
}

public abstract class BatchMutator<TGenotype, TEncoding> : IMutator<TGenotype, TEncoding>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding);

  IReadOnlyList<TGenotype> IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return Mutate(parent, random, encoding);
  }
}
