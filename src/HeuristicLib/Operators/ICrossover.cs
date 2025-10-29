using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TGenotype> : ICrossover<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random);
}

public interface ICrossover<TGenotype, in TEncoding> : ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding);
}

public interface ICrossover<TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  IReadOnlyList<TGenotype> Cross(IReadOnlyList<(TGenotype, TGenotype)> parents, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}
