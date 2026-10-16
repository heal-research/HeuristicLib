using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public abstract record StatelessRefiner<TCandidate, TSearchSpace, TProblem>
    : Refiner<TCandidate, TSearchSpace, TProblem>, IRefinementExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public sealed override ExecutorFactory<IRefinementExecutor<TCandidate, TSearchSpace, TProblem>> CreateExecutorFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessRefiner<TCandidate, TSearchSpace>
    : Refiner<TCandidate, TSearchSpace>, IRefinementExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public sealed override ExecutorFactory<IRefinementExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateExecutorFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> IRefinementExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Refine(candidates, random, searchSpace);
}

public abstract record StatelessRefiner<TCandidate>
    : Refiner<TCandidate>, IRefinementExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public sealed override ExecutorFactory<IRefinementExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutorFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> IRefinementExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Refine(candidates, random);
}
