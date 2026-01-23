using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Executors;

public interface IRepeatedExecutor<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState {
  IReadOnlyList<IAsyncEnumerable<TAlgorithmState>> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, CancellationToken cancellationToken = default);
}
