using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record StatelessMutator<TCandidate, TSearchSpace, TProblem>
    : Mutator<TCandidate, TSearchSpace, TProblem>, IMutatorExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public sealed override ExecutionFactory<IMutatorExecution<TCandidate, TSearchSpace, TProblem>> CreateExecutionFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessMutator<TCandidate, TSearchSpace>
    : Mutator<TCandidate, TSearchSpace>, IMutatorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public sealed override ExecutionFactory<IMutatorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateExecutionFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> IMutatorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Mutate(parents, random, searchSpace);
}

public abstract record StatelessMutator<TCandidate>
    : Mutator<TCandidate>, IMutatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public sealed override ExecutionFactory<IMutatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutionFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> IMutatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Mutate(parents, random);
}
