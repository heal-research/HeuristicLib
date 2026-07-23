using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record WrappingMutator<TCandidate, TSearchSpace, TProblem>
    : Mutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IMutator<TCandidate, TSearchSpace, TProblem> InnerMutator { get; }

    protected WrappingMutator(IMutator<TCandidate, TSearchSpace, TProblem> innerMutator)
    {
        InnerMutator = innerMutator;
    }

    protected sealed override IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateMutatorInstance(ExecutionInstanceRegistry registry) =>
        CreateMutatorInstance(registry.Resolve(InnerMutator));

    protected abstract WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateMutatorInstance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> innerMutator);
}

public abstract class WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(IMutatorInstance<TCandidate, TSearchSpace, TProblem> innerMutator)
    : MutatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IMutatorInstance<TCandidate, TSearchSpace, TProblem> InnerMutator { get; } = innerMutator;
}
