using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public interface ICreator<out TGenotype> : ICreator<TGenotype, ISearchSpace<TGenotype>>
{
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random);
}

public interface ICreator<out TGenotype, in TSearchSpace> : ICreator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace encoding);
}

public interface ICreator<out TGenotype, in TSearchSpace, in TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem);
  TGenotype Create(IRandomNumberGenerator random, TSearchSpace encoding, TProblem problem) => Create(1, random, encoding, problem)[0];
}
