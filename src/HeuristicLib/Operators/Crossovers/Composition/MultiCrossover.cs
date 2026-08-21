using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record MultiCrossover<TCandidate, TSearchSpace, TProblem>
    : Crossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiCrossover(IReadOnlyList<ICrossover<TCandidate, TSearchSpace, TProblem>> childCrossovers)
    {
        ChildCrossovers = childCrossovers.ToValueArray();
    }

    public ValueArray<ICrossover<TCandidate, TSearchSpace, TProblem>> ChildCrossovers { get; init; }

    public sealed override ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance([.. ChildCrossovers.Select(instanceRegistry.Resolve)]);

    protected abstract MultiCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> childCrossovers);
}

public abstract class MultiCrossoverInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> childCrossovers)
    : CrossoverInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> ChildCrossovers { get; } = childCrossovers;
}
