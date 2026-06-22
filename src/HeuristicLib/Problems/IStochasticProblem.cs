using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public interface IStochasticProblem<TSolution, out TSearchSpace> : IProblem<TSolution, TSearchSpace> where TSearchSpace : class, ISearchSpace<TSolution>;
