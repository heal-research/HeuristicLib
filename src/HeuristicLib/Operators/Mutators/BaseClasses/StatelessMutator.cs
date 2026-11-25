using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record StatelessMutator<TCandidate, TSearchSpace, TProblem> : Mutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override IMutationExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope) => new Executor(this, scope.Problem);

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Executor(StatelessMutator<TCandidate, TSearchSpace, TProblem> configuration, TProblem problem) : MutationExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) => configuration.Mutate(parents, random, problem.SearchSpace, problem);
    }
}

public abstract record StatelessMutator<TCandidate, TSearchSpace> : Mutator<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected sealed override IMutationExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TRunSearchState> scope) => new Executor(this, scope.SearchSpace);

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    private sealed class Executor(StatelessMutator<TCandidate, TSearchSpace> configuration, TSearchSpace searchSpace) : MutationExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) => configuration.Mutate(parents, random, searchSpace);
    }
}

public abstract record StatelessMutator<TCandidate> : Mutator<TCandidate>, IMutationExecutor<TCandidate>
{
    protected sealed override IMutationExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TRunSearchState> scope) => this;

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random);
}
