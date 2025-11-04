using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Mutator;

public interface IMutator<TGenotype> : IMutator<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random);
}

public interface IMutator<TGenotype, in TEncoding> : IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding);
}

public interface IMutator<TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}
