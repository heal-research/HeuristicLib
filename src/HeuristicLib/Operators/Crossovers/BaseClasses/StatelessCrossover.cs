using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record StatelessCrossover<TCandidate, TSearchSpace, TProblem>
    : Crossover<TCandidate, TSearchSpace, TProblem>, ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public sealed override ExecutorFactory<ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>> CreateExecutorFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessCrossover<TCandidate, TSearchSpace>
    : Crossover<TCandidate, TSearchSpace>, ICrossoverExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public sealed override ExecutorFactory<ICrossoverExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateExecutorFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> ICrossoverExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Cross(parents, random, searchSpace);
}

public abstract record StatelessCrossover<TCandidate>
    : Crossover<TCandidate>, ICrossoverExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public sealed override ExecutorFactory<ICrossoverExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutorFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> ICrossoverExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Cross(parents, random);
}
