using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record MultiCrossover<TCandidate, TSearchSpace, TProblem>
    : Crossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ValueArray<ICrossover<TCandidate, TSearchSpace, TProblem>> InnerCrossovers { get; }

    protected MultiCrossover(IReadOnlyList<ICrossover<TCandidate, TSearchSpace, TProblem>> innerCrossovers)
    {
        InnerCrossovers = innerCrossovers.ToValueArray();
    }

    protected sealed override ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(ExecutionInstanceRegistry registry) =>
        CreateCrossoverInstance([.. InnerCrossovers.Select(registry.Resolve)]);

    protected abstract MultiCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> innerCrossovers);
}

public abstract class MultiCrossoverInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> innerCrossovers)
    : CrossoverInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> InnerCrossovers { get; } = innerCrossovers;
}
