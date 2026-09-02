using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <remarks>
/// Derive directly from this base when the creator owns child execution instances or needs direct control over its execution structure.
/// Use <see cref="StatelessCreator{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulCreator{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// <para>
/// The type arguments are the search space and problem this creator is written for. The base bridges to whatever a
/// run requests, and a request the creator was not written for is reported when the execution graph is built. A
/// creator that owns children stays agnostic and derives from <see cref="WrappingCreator{TCandidate}"/> or
/// <see cref="MultiCreator{TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Creator<TCandidate, TSearchSpace, TProblem>
    : ICreator<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract ICreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

    ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> ICreator<TCandidate>.CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (CreateExecutionInstance(instanceRegistry) is not ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> instance)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is written for {typeof(TSearchSpace).Name} and {typeof(TProblem).Name}, and cannot run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
        }

        return instance;
    }
}

public abstract record Creator<TCandidate, TSearchSpace>
    : Creator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Creator<TCandidate>
    : Creator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

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
