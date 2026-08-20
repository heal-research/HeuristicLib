using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public abstract record MultiRefiner<TCandidate, TSearchSpace, TProblem>
    : Refiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiRefiner(IReadOnlyList<IRefiner<TCandidate, TSearchSpace, TProblem>> childRefiners)
    {
        ChildRefiners = childRefiners.ToValueArray();
    }

    public ValueArray<IRefiner<TCandidate, TSearchSpace, TProblem>> ChildRefiners { get; init; }

    public sealed override IRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance([.. ChildRefiners.Select(instanceRegistry.Resolve)]);

    protected abstract MultiRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<IRefinerInstance<TCandidate, TSearchSpace, TProblem>> childRefiners);
}

public abstract class MultiRefinerInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IRefinerInstance<TCandidate, TSearchSpace, TProblem>> childRefiners)
    : RefinerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IRefinerInstance<TCandidate, TSearchSpace, TProblem>> ChildRefiners { get; } = childRefiners;
}
