using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

/// <remarks>
/// Derive directly from this base when the selector owns child execution instances or needs direct control over its execution structure.
/// Use <see cref="StatelessSelector{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulSelector{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// </remarks>
public abstract record Selector<TCandidate, TSearchSpace, TProblem>
    : ISelector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract ISelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract record Selector<TCandidate, TSearchSpace>
    : Selector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Selector<TCandidate>
    : Selector<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class SelectorInstance<TCandidate, TSearchSpace, TProblem>
    : ISelectorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class SelectorInstance<TCandidate, TSearchSpace>
    : ISelectorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<EvaluatedCandidate<TCandidate>> ISelectorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Select(population, objective, count, random, searchSpace);
}

public abstract class SelectorInstance<TCandidate>
    : ISelectorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random);

    IReadOnlyList<EvaluatedCandidate<TCandidate>> ISelectorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Select(population, objective, count, random);
}
