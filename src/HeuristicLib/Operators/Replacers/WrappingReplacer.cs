using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public abstract record WrappingReplacer<TCandidate, TSearchSpace, TProblem>
    : Replacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IReplacer<TCandidate, TSearchSpace, TProblem> InnerReplacer { get; }

    protected WrappingReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> innerReplacer)
    {
        InnerReplacer = innerReplacer;
    }

    protected sealed override IReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateReplacerInstance(ExecutionInstanceRegistry registry) =>
        CreateReplacerInstance(registry.Resolve(InnerReplacer));

    protected abstract WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateReplacerInstance(IReplacerInstance<TCandidate, TSearchSpace, TProblem> innerReplacer);
}

public abstract class WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem>(IReplacerInstance<TCandidate, TSearchSpace, TProblem> innerReplacer)
    : ReplacerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IReplacerInstance<TCandidate, TSearchSpace, TProblem> InnerReplacer { get; } = innerReplacer;
}
