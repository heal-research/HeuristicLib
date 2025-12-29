using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class Algorithm<TGenotype, TSearchSpace, TProblem, TIterationState> : IAlgorithm<TGenotype, TSearchSpace, TProblem, TIterationState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TIterationState : class, IIterationState {
  public abstract TIterationState Execute(TProblem problem, TSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null);
  //IIterationState IAlgorithm<TGenotype, TSearchSpace, TProblem>.Execute(TProblem problem, TSearchSpace? searchSpace, IRandomNumberGenerator? random) => Execute(problem, searchSpace, random);
}
