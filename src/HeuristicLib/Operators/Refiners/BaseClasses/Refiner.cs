using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

/// <remarks>
/// Derive directly from this base when the refiner owns child execution instances or needs direct control over its execution structure.
/// Use <see cref="StatelessRefiner{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulRefiner{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// </remarks>
public abstract record Refiner<TCandidate, TSearchSpace, TProblem>
    : IRefiner<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

    IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> IRefiner<TCandidate>.CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        if (CreateExecutionInstance(instanceRegistry) is not IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> instance)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is written for {typeof(TSearchSpace).Name} and {typeof(TProblem).Name}, and cannot run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
        }

        return instance;
    }
}

public abstract record Refiner<TCandidate, TSearchSpace>
    : Refiner<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Refiner<TCandidate>
    : Refiner<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class RefinerInstance<TCandidate, TSearchSpace, TProblem>
    : IRefinerInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class RefinerInstance<TCandidate, TSearchSpace>
    : IRefinerInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> IRefinerInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Refine(candidates, random, searchSpace);
}

public abstract class RefinerInstance<TCandidate>
    : IRefinerInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> IRefinerInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Refine(candidates, random);
}
