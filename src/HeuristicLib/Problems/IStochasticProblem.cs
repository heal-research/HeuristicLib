using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public interface IStochasticProblem<TCandidate, out TSearchSpace> : IProblem<TCandidate, TSearchSpace> where TSearchSpace : class, ISearchSpace<TCandidate>;
