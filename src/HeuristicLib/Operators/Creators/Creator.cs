using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <remarks>
/// Derive directly from this base when the creator owns child execution instances or needs direct control over its execution structure.
/// Use <see cref="StatelessCreator{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulCreator{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// </remarks>
public abstract record Creator<TCandidate, TSearchSpace, TProblem>
    : ICreator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected abstract ICreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(ExecutionInstanceRegistry registry);

    ICreatorInstance<TCandidate, TSearchSpace, TProblem> IExecutionInstanceResolvable<ICreatorInstance<TCandidate, TSearchSpace, TProblem>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateCreatorInstance(instanceRegistry);
}

public abstract record Creator<TCandidate, TSearchSpace>
    : ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected abstract ICreatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateCreatorInstance(ExecutionInstanceRegistry registry);

    ICreatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> IExecutionInstanceResolvable<ICreatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateCreatorInstance(instanceRegistry);
}

public abstract record Creator<TCandidate>
    : ICreator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected abstract ICreatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateCreatorInstance(ExecutionInstanceRegistry registry);

    ICreatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> IExecutionInstanceResolvable<ICreatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>>.CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateCreatorInstance(instanceRegistry);
}

public abstract class CreatorInstance<TCandidate, TSearchSpace, TProblem>
    : ICreatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class CreatorInstance<TCandidate, TSearchSpace>
    : ICreatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> ICreatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Create(count, random, searchSpace);
}

public abstract class CreatorInstance<TCandidate>
    : ICreatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> ICreatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Create(count, random);
}
