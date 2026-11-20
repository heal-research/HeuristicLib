using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record StatelessCrossover<TCandidate, TSearchSpace, TProblem> : Crossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override ICrossoverExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope) => new Executor(this, scope.Problem);

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Executor(StatelessCrossover<TCandidate, TSearchSpace, TProblem> configuration, TProblem problem) : CrossoverExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random) => configuration.Cross(parents, random, problem.SearchSpace, problem);
    }
}

public abstract record StatelessCrossover<TCandidate, TSearchSpace> : Crossover<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected sealed override ICrossoverExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TRunSearchState> scope) => new Executor(this, scope.SearchSpace);

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    private sealed class Executor(StatelessCrossover<TCandidate, TSearchSpace> configuration, TSearchSpace searchSpace) : CrossoverExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random) => configuration.Cross(parents, random, searchSpace);
    }
}

public abstract record StatelessCrossover<TCandidate> : Crossover<TCandidate>, ICrossoverExecutor<TCandidate>
{
    protected sealed override ICrossoverExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TRunSearchState> scope) => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random);
}
