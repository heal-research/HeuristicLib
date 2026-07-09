using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public static class NeighborhoodExecutionInstanceExtensions
{
    public static INeighborhoodInstance<TCandidate, TSearchSpace, TProblem, TMove> CreateStandaloneExecutionInstance<TCandidate, TSearchSpace, TProblem, TMove>(
        this INeighborhood<TCandidate, TSearchSpace, TProblem, TMove> neighborhood)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => neighborhood.CreateExecutionInstance(new ExecutionInstanceRegistry(StandaloneNeighborhoodRun.Instance));

    private sealed class StandaloneNeighborhoodRun : Run
    {
        public static readonly StandaloneNeighborhoodRun Instance = new();

        private StandaloneNeighborhoodRun()
        {
        }
    }
}
