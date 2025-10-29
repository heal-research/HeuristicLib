using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<out TGenotype> : ICreator<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);
}

public interface ICreator<out TGenotype, in TEncoding> : ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding);
}

public interface ICreator<out TGenotype, in TEncoding, in TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding, TProblem problem);
  TGenotype Create(IRandomNumberGenerator random, TEncoding encoding, TProblem problem) => Create(1, random, encoding, problem)[0];
}
