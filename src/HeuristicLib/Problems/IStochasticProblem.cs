using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public interface IStochasticProblem<in TISolution, out TSearchSpace> : IProblem<TISolution, TSearchSpace> where TSearchSpace : class, ISearchSpace<TISolution>;
