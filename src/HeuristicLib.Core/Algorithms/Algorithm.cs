using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmResult>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmResult>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmResult : class, IAlgorithmState
{
  public abstract TAlgorithmResult Execute(TProblem problem, TSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null);
  IAlgorithmState IAlgorithm<TGenotype, TSearchSpace, TProblem>.Execute(TProblem problem, TSearchSpace? searchSpace, IRandomNumberGenerator? random) => Execute(problem, searchSpace, random);
}
