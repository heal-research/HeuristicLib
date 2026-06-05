using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public static class NeighborhoodExecutionInstanceExtensions
{
    public static INeighborhoodInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateStandaloneExecutionInstance<TGenotype, TSearchSpace, TProblem, TMove>(
        this INeighborhood<TGenotype, TSearchSpace, TProblem, TMove> neighborhood)
        where TSearchSpace : class, ISearchSpace<TGenotype>
        where TProblem : class, IProblem<TGenotype, TSearchSpace>
        => neighborhood.CreateExecutionInstance(new ExecutionInstanceRegistry(StandaloneNeighborhoodRun.Instance));

    private sealed class StandaloneNeighborhoodRun : Run
    {
        public static readonly StandaloneNeighborhoodRun Instance = new();

        private StandaloneNeighborhoodRun()
        {
        }
    }
}
