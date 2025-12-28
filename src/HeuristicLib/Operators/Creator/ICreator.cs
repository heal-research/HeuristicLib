using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Creator;

public interface ICreator<out TGenotype> : ICreator<TGenotype, IEncoding<TGenotype>> {
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);
}

public interface ICreator<out TGenotype, in TEncoding> : ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype> {
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TEncoding encoding);
}

