using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record EliteSelector<TCandidate, TSearchSpace, TProblem>
    : Selector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public EliteSelector(ISelector<TCandidate, TSearchSpace, TProblem> selectorForRemaining)
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
    public ISelector<TCandidate, TSearchSpace, TProblem> SelectorForRemaining { get; init; }

    /// <summary>
    /// Gets the number of best candidates taken before the remaining places are filled. The expected value is
    /// nonnegative.
    /// </summary>
    /// <remarks>
    /// The selection never returns more than the requested count, so a value above that count is capped and leaves
    /// no remaining places. A nonpositive value takes no elites.
    /// </remarks>
    public int Elites { get; init; } = 1;

    public override SelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(instanceRegistry.Resolve(SelectorForRemaining), Elites);

    private sealed class Instance(ISelectorInstance<TCandidate, TSearchSpace, TProblem> selectorForRemaining, int elites)
        : SelectorInstance<TCandidate, TSearchSpace, TProblem>
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
    public static EliteSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector, int elites = 1)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(selector) { Elites = elites };
}

public static class EliteSelectorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public EliteSelector<TCandidate, TSearchSpace, TProblem> WithElites(int elites = 1) => EliteSelector.Create(selector, elites);
    }
}
