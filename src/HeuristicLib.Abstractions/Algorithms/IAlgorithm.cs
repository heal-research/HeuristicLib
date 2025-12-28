using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TSearchSpace, in TProblem, out TAlgorithmResult> 
/*: IAlgorithm<TGenotype, TSearchSpace, TProblem>*/
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmResult : IIterationResult {
  TAlgorithmResult Execute(TProblem problem, TSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null);
}

// public interface IAlgorithm<TGenotype, in TSearchSpace, in TProblem>
//   where TSearchSpace : class, IEncoding<TGenotype>
//   where TProblem : class, IProblem<TGenotype, TSearchSpace> {
//   IIterationResult Execute(TProblem problem, TSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null);
// }
