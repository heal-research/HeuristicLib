using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public interface ICreator<out TGenotype, in TSearchSpace, in TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> 
{
  IReadOnlyList<TGenotype> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  // ToDo: do we really want this heere?
  TGenotype Create(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => Create(1, random, searchSpace, problem)[0];
}

