using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creator;

public interface ICreator<out TGenotype> : ICreator<TGenotype, ISearchSpace<TGenotype>> {
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);
}

public interface ICreator<out TGenotype, in TSearchSpace> : ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace);
}

