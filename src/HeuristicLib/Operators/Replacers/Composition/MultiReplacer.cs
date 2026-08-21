using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract record MultiReplacer<TCandidate, TSearchSpace, TProblem>
    : Replacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiReplacer(IReadOnlyList<IReplacer<TCandidate, TSearchSpace, TProblem>> childReplacers)
    {
        ChildReplacers = childReplacers.ToValueArray();
    }

    public ValueArray<IReplacer<TCandidate, TSearchSpace, TProblem>> ChildReplacers { get; init; }

    public sealed override IReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance([.. ChildReplacers.Select(instanceRegistry.Resolve)]);

    protected abstract MultiReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> childReplacers);
}

public abstract class MultiReplacerInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> childReplacers)
    : ReplacerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> ChildReplacers { get; } = childReplacers;
}
