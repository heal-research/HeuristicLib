using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

/// <remarks>
/// Derive directly from this base when the selector needs direct control over its execution structure.
/// Use <see cref="StatelessSelector{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulSelector{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// <para>
/// The type arguments are the search space and problem this selector is written for. The base bridges to whatever a
/// run requests, and a request the selector was not written for is reported when the execution graph is built. A
/// selector that owns children stays agnostic and derives from <see cref="WrappingOperator{Selection, TCandidate}"/> or
/// <see cref="MultiOperator{Selection, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Selector<TCandidate, TSearchSpace, TProblem>
    : Operator<Selection, TCandidate, TSearchSpace, TProblem, ISelectionExecutor<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Selector<TCandidate, TSearchSpace>
    : Selector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Selector<TCandidate>
    : Selector<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class SelectionExecutor<TCandidate, TSearchSpace, TProblem>
    : ISelectionExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class SelectionExecutor<TCandidate, TSearchSpace>
    : ISelectionExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<EvaluatedCandidate<TCandidate>> ISelectionExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Select(population, objective, count, random, searchSpace);
}

public abstract class SelectionExecutor<TCandidate>
    : ISelectionExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random);

    IReadOnlyList<EvaluatedCandidate<TCandidate>> ISelectionExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Select(population, objective, count, random);
}
