using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

/// <remarks>
/// Derive directly from this base when the selector needs direct control over its execution structure.
/// Use <see cref="StatelessSelector{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// <para>
/// The type arguments are the search space and problem this selector is written for. The base bridges to whatever a
/// run requests, and a request the selector was not written for is reported when the execution graph is built. A
/// selector that owns children stays agnostic and derives from <see cref="WrappingOperator{Selection, TCandidate}"/> or
/// <see cref="MultiOperator{Selection, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Selector<TCandidate, TSearchSpace, TProblem>
    : Operator<Selection, TCandidate, TSearchSpace, TProblem, ISelectionExecutor<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Selector<TCandidate, TSearchSpace>
    : Selector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Selector<TCandidate>
    : Selector<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class SelectionExecutor<TCandidate, TSearchSpace, TProblem>
    : ISelectionExecutor<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected TProblem Problem { get; }
    protected TSearchSpace SearchSpace { get; }

    protected SelectionExecutor(TProblem problem)
    {
        Problem = problem;
        SearchSpace = problem.SearchSpace;
    }

    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random);
}

public abstract class SelectionExecutor<TCandidate, TSearchSpace>
    : ISelectionExecutor<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected TSearchSpace SearchSpace { get; }

    protected SelectionExecutor(TSearchSpace searchSpace)
    {
        SearchSpace = searchSpace;
    }

    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random);
}

public abstract class SelectionExecutor<TCandidate>
    : ISelectionExecutor<TCandidate>
{
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random);
}
