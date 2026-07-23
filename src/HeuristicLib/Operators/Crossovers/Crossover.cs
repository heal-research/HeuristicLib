using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// Derive directly from this base when the crossover owns child execution instances or needs direct control over its execution structure.
/// Use <see cref="StatelessCrossover{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulCrossover{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// </remarks>
public abstract record Crossover<TCandidate, TSearchSpace, TProblem>
    : ICrossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected abstract ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(ExecutionInstanceRegistry registry);

    ICrossoverInstance<TCandidate, TSearchSpace, TProblem> IExecutionInstanceResolvable<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateCrossoverInstance(instanceRegistry);
}

public abstract record Crossover<TCandidate, TSearchSpace>
    : ICrossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected abstract ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateCrossoverInstance(ExecutionInstanceRegistry registry);

    ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> IExecutionInstanceResolvable<ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateCrossoverInstance(instanceRegistry);
}

public abstract record Crossover<TCandidate>
    : ICrossover<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected abstract ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateCrossoverInstance(ExecutionInstanceRegistry registry);

    ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> IExecutionInstanceResolvable<ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateCrossoverInstance(instanceRegistry);
}

public abstract class CrossoverInstance<TCandidate, TSearchSpace, TProblem>
    : ICrossoverInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class CrossoverInstance<TCandidate, TSearchSpace>
    : ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> ICrossoverInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Cross(parents, random, searchSpace);
}

public abstract class CrossoverInstance<TCandidate>
    : ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> ICrossoverInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Cross(parents, random);
}
