using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ISelector<TGenotype> : ISelector<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random);
}

public interface ISelector<TGenotype, in TEncoding> : ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding);
}

public interface ISelector<TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  IReadOnlyList<Solution<TGenotype>> Select(IReadOnlyList<Solution<TGenotype>> population, Objective objective, int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
}
