using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record EliteSelector<TCandidate>
    : ISelector<TCandidate>
{
    public EliteSelector(ISelector<TCandidate> selectorForRemaining)
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
    public ISelector<TCandidate> SelectorForRemaining { get; init; }

    /// <summary>
    /// Gets the number of best candidates taken before the remaining places are filled. The expected value is
    /// nonnegative.
    /// </summary>
    /// <remarks>
    /// The selection never returns more than the requested count, so a value above that count is capped and leaves
    /// no remaining places. A nonpositive value takes no elites.
    /// </remarks>
    public int Elites { get; init; } = 1;

    public ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        new Instance<TRunSearchSpace, TRunProblem>(instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(SelectorForRemaining), Elites);

    private sealed class Instance<TSearchSpace, TProblem>(ISelectorInstance<TCandidate, TSearchSpace, TProblem> selectorForRemaining, int elites)
        : SelectorInstance<TCandidate, TSearchSpace, TProblem>
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
    public static EliteSelector<TCandidate> Create<TCandidate>(ISelector<TCandidate> selector, int elites = 1) => new(selector) { Elites = elites };
}

public static class EliteSelectorExtensions
{
    extension<TCandidate>(ISelector<TCandidate> selector)
    {
        public EliteSelector<TCandidate> CombinedWithElites(int elites = 1) => EliteSelector.Create(selector, elites);
    }
}
