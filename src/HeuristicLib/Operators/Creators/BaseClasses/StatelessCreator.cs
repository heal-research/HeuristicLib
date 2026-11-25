using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record StatelessCreator<TCandidate, TSearchSpace, TProblem> : Creator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override ICreationExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope) => new Executor(this, scope.Problem);

    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Executor(StatelessCreator<TCandidate, TSearchSpace, TProblem> configuration, TProblem problem) : CreationExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random) => configuration.Create(count, random, problem.SearchSpace, problem);
    }
}

public abstract record StatelessCreator<TCandidate, TSearchSpace> : Creator<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected sealed override ICreationExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TRunSearchState> scope) => new Executor(this, scope.SearchSpace);

    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

    private sealed class Executor(StatelessCreator<TCandidate, TSearchSpace> configuration, TSearchSpace searchSpace) : CreationExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random) => configuration.Create(count, random, searchSpace);
    }
}

public abstract record StatelessCreator<TCandidate> : Creator<TCandidate>, ICreationExecutor<TCandidate>
{
    protected sealed override ICreationExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TRunSearchState> scope) => this;

    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random);
}
