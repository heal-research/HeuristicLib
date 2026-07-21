using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

[Equatable]
public abstract partial record MultiReplacer<TCandidate, TSearchSpace, TProblem>
    : Replacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality]
    protected ImmutableArray<IReplacer<TCandidate, TSearchSpace, TProblem>> InnerReplacers { get; }

    protected MultiReplacer(ImmutableArray<IReplacer<TCandidate, TSearchSpace, TProblem>> innerReplacers)
    {
        InnerReplacers = innerReplacers;
    }

    protected sealed override IReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateReplacerInstance(ExecutionInstanceRegistry registry) =>
        CreateReplacerInstance([.. InnerReplacers.Select(registry.Resolve)]);

    protected abstract MultiReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateReplacerInstance(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> innerReplacers);
}

public abstract class MultiReplacerInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> innerReplacers)
    : ReplacerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> InnerReplacers { get; } = innerReplacers;
}
