using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>One successful selection call, with the requested count and the candidates it selected.</summary>
/// <remarks>
/// The search space and problem are read at the candidate's interfaces, so an observation fits every run over the
/// candidate.
/// </remarks>
public sealed record SelectionObservation<TCandidate>(
    IOperator<Selection, TCandidate> Selector,
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Population,
    ObjectiveDirections Objective,
    int Count,
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Selected,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Selection, TCandidate, SelectionObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Selection, TCandidate, SelectionObservation<TCandidate>>.Wrap<TSearchSpace, TProblem, TSearchState>(
        IExecutor executor, IOperator<Selection, TCandidate> source, Action<SelectionObservation<TCandidate>> record, TProblem problem) =>
        new Executor<TSearchSpace, TProblem>((ISelectionExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor<TSearchSpace, TProblem>(ISelectionExecutor<TCandidate> executor,
        IOperator<Selection, TCandidate> selector, Action<SelectionObservation<TCandidate>> observe, TProblem problem)
        : SelectionExecutor<TCandidate, TSearchSpace, TProblem>(problem), IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        {
            var selected = executor.Select(population, objective, count, random);
            observe(new SelectionObservation<TCandidate>(selector, population, objective, count, selected, SearchSpace, Problem));
            return selected;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}

public static class SelectionObservations
{
    extension<TCandidate>(IOperator<Selection, TCandidate> selector)
    {
        /// <summary>The observations of this selector's successful calls.</summary>
        public InvocationSource<Selection, TCandidate, SelectionObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Selection, TCandidate, SelectionObservation<TCandidate>>(selector);

        /// <summary>Records this selector's successful calls into the instrument, wherever this configuration is placed.</summary>
        public Observed<Selection, TCandidate, SelectionObservation<TCandidate>> Observed(IObservationInstrument<SelectionObservation<TCandidate>> instrument) =>
            new(selector, instrument);
    }
}
