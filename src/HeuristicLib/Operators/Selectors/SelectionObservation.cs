using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
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
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem);

file sealed class SelectionObservationAdvice<TCandidate>(Action<SelectionObservation<TCandidate>> observe) : SelectorAdvice<TCandidate>
{
    protected override ISelectionExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        ISelectionExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Selection, TCandidate> selector) =>
        new Executor<TSearchSpace, TProblem>(executor, selector, observe);

    private sealed class Executor<TSearchSpace, TProblem>(ISelectionExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IOperator<Selection, TCandidate> selector, Action<SelectionObservation<TCandidate>> observe)
        : ISelectionExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective,
            int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var selected = executor.Select(population, objective, count, random, searchSpace, problem);
            observe(new SelectionObservation<TCandidate>(selector, population, objective, count, selected, searchSpace, problem));
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
        public ObservationSource<SelectionObservation<TCandidate>> Invocations =>
            ObservationSource.Create<SelectionObservation<TCandidate>>(selector, observe => Aspect.Create(Pointcut.Reference(selector), new SelectionObservationAdvice<TCandidate>(observe)));
    }
}
