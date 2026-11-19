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
/// candidate. Read the bound objective through <c>Problem.Objective</c>.
/// </remarks>
public sealed record SelectionObservation<TCandidate>(
    IOperator<Selection, TCandidate> Selector,
    ValueArray<EvaluatedCandidate<TCandidate>> Population,
    int Count,
    ValueArray<EvaluatedCandidate<TCandidate>> Selected,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IInvocationObservation<Selection, TCandidate, SelectionObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Selection, TCandidate, SelectionObservation<TCandidate>>.Wrap<TSearchSpace, TProblem, TSearchState>(
        IExecutor executor, IOperator<Selection, TCandidate> source, Action<SelectionObservation<TCandidate>> record, TProblem problem) =>
        new Executor((ISelectionExecutor<TCandidate>)executor, source, record, problem);

    private sealed class Executor(ISelectionExecutor<TCandidate> executor,
        IOperator<Selection, TCandidate> selector, Action<SelectionObservation<TCandidate>> observe, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        : SelectionExecutor<TCandidate>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, int count, IRandomNumberGenerator random)
        {
            var selected = executor.Select(population, count, random);
            observe(new SelectionObservation<TCandidate>(selector, population.ToValueArray(), count, selected.ToValueArray(), problem.SearchSpace, problem));
            return selected;
        }
    }
}

public static class SelectionObservations
{
    extension<TCandidate>(IOperator<Selection, TCandidate> selector)
    {
        /// <summary>The observations of this selector's successful calls.</summary>
        public InvocationSource<Selection, TCandidate, SelectionObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Selection, TCandidate, SelectionObservation<TCandidate>>(selector);

        /// <summary>Records this selector's successful calls into the receiver, wherever this configuration is placed.</summary>
        public Observed<Selection, TCandidate, SelectionObservation<TCandidate>> Observed(IObservationReceiver<SelectionObservation<TCandidate>> receiver) =>
            new(selector, receiver);
    }
}
