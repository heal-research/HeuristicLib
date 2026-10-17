using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record StatelessMutator<TCandidate, TSearchSpace, TProblem>
    : Mutator<TCandidate, TSearchSpace, TProblem>, IMutationExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override ExecutorFactory<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> CreateFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessMutator<TCandidate, TSearchSpace>
    : Mutator<TCandidate, TSearchSpace>, IMutationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected sealed override ExecutorFactory<IMutationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> IMutationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Mutate(parents, random, searchSpace);
}

public abstract record StatelessMutator<TCandidate>
    : Mutator<TCandidate>, IMutationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected sealed override ExecutorFactory<IMutationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> IMutationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Mutate(parents, random);
}
