using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public abstract record WrappingRefiner<TCandidate, TSearchSpace, TProblem>
    : Refiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingRefiner(IRefiner<TCandidate, TSearchSpace, TProblem> childRefiner)
    {
        ChildRefiner = childRefiner;
    }

    public IRefiner<TCandidate, TSearchSpace, TProblem> ChildRefiner { get; init; }

    public sealed override IRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance(instanceRegistry.Resolve(ChildRefiner));

    protected abstract WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner);
}

public abstract class WrappingRefinerInstance<TCandidate, TSearchSpace, TProblem>(IRefinerInstance<TCandidate, TSearchSpace, TProblem> childRefiner)
    : RefinerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IRefinerInstance<TCandidate, TSearchSpace, TProblem> ChildRefiner { get; } = childRefiner;
}
