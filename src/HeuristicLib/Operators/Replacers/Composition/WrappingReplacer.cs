using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract record WrappingReplacer<TCandidate, TSearchSpace, TProblem>
    : Replacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> childReplacer)
    {
        ChildReplacer = childReplacer;
    }

    public IReplacer<TCandidate, TSearchSpace, TProblem> ChildReplacer { get; init; }

    public sealed override IReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance(instanceRegistry.Resolve(ChildReplacer));

    protected abstract WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IReplacerInstance<TCandidate, TSearchSpace, TProblem> childReplacer);
}

public abstract class WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem>(IReplacerInstance<TCandidate, TSearchSpace, TProblem> childReplacer)
    : ReplacerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IReplacerInstance<TCandidate, TSearchSpace, TProblem> ChildReplacer { get; } = childReplacer;
}
