using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

[Equatable]
public abstract partial record MultiCrossover<TCandidate, TSearchSpace, TProblem>
    : Crossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality]
    protected ImmutableArray<ICrossover<TCandidate, TSearchSpace, TProblem>> InnerCrossovers { get; }

    protected MultiCrossover(ImmutableArray<ICrossover<TCandidate, TSearchSpace, TProblem>> innerCrossovers)
    {
        InnerCrossovers = innerCrossovers;
    }

    protected sealed override ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(IExecutionInstanceResolver resolver) =>
        CreateCrossoverInstance([.. InnerCrossovers.Select(resolver.Resolve)]);

    protected abstract MultiCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> innerCrossovers);
}

public abstract class MultiCrossoverInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> innerCrossovers)
    : CrossoverInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> InnerCrossovers { get; } = innerCrossovers;
}
