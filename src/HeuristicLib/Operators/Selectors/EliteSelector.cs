using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record EliteSelector<TCandidate>
    : Operator<Selection, TCandidate>
{
    public EliteSelector(IOperator<Selection, TCandidate> selectorForRemaining)
    {
        SelectorForRemaining = selectorForRemaining;
    }

    /// <summary>
    /// Gets the selector that fills the places remaining after the elites have been taken. It is asked for the
    /// reduced count rather than for the complete selection.
    /// </summary>
    /// <remarks>
    /// It is not called at all when the elites already fill the requested count, so it consumes no random draws in
    /// that case.
    /// </remarks>
    public IOperator<Selection, TCandidate> SelectorForRemaining { get; init; }

    /// <summary>
    /// Gets the number of best candidates taken before the remaining places are filled. The expected value is
    /// nonnegative.
    /// </summary>
    /// <remarks>
    /// The selection never returns more than the requested count, so a value above that count is capped and leaves
    /// no remaining places. A nonpositive value takes no elites.
    /// </remarks>
    public int Elites { get; init; } = 1;

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) =>
        new Executor<TRunSearchSpace, TRunProblem>(scope.Resolve(SelectorForRemaining), Elites);

    private sealed class Executor<TSearchSpace, TProblem>(ISelectionExecutor<TCandidate, TSearchSpace, TProblem> selectorForRemaining, int elites)
        : SelectionExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var selectedElites = BestSelector.Select(population, objective, Math.Min(elites, count));
            var remainingCount = count - selectedElites.Count;
            if (remainingCount <= 0)
                return selectedElites;

            var selectedRemaining = selectorForRemaining.Select(population, objective, remainingCount, random, searchSpace, problem);

            return selectedElites.Concat(selectedRemaining).ToArray();
        }
    }
}

public static class EliteSelector
{
    public static EliteSelector<TCandidate> Create<TCandidate>(IOperator<Selection, TCandidate> selector, int elites = 1) => new(selector) { Elites = elites };
}

public static class EliteSelectorExtensions
{
    extension<TCandidate>(IOperator<Selection, TCandidate> selector)
    {
        public EliteSelector<TCandidate> CombinedWithElites(int elites = 1) => EliteSelector.Create(selector, elites);
    }
}
