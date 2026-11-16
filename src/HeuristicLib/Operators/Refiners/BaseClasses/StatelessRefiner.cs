using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public abstract record StatelessRefiner<TCandidate, TSearchSpace, TProblem> : Refiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override IRefinementExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope) => new Executor(this, scope.Problem);

    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Executor(StatelessRefiner<TCandidate, TSearchSpace, TProblem> configuration, TProblem problem) : RefinementExecutor<TCandidate, TSearchSpace, TProblem>(problem)
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) => configuration.Refine(candidates, random, SearchSpace, Problem);
    }
}

public abstract record StatelessRefiner<TCandidate, TSearchSpace> : Refiner<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected sealed override IRefinementExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TRunSearchState> scope) => new Executor(this, scope.SearchSpace);

    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace);

    private sealed class Executor(StatelessRefiner<TCandidate, TSearchSpace> configuration, TSearchSpace searchSpace) : RefinementExecutor<TCandidate, TSearchSpace>(searchSpace)
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) => configuration.Refine(candidates, random, SearchSpace);
    }
}

public abstract record StatelessRefiner<TCandidate> : Refiner<TCandidate>, IRefinementExecutor<TCandidate>
{
    protected sealed override IRefinementExecutor<TCandidate> CreateExecutor<TRunSearchState>(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TRunSearchState> scope) => this;

    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}
