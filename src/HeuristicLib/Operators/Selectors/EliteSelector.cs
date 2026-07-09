using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

// ToDo: If we assume that a selector cannot select the whole requested number of solutions, the EliteSelector could simply be a PipelineSelector with a BestSelector and then another selector for the remaining.
public record EliteSelector<TCandidate, TSearchSpace, TProblem>
  : WrappingSelector<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly int elites;

    public ISelector<TCandidate, TSearchSpace, TProblem> SelectorForRemaining => InnerSelector;

    public EliteSelector(ISelector<TCandidate, TSearchSpace, TProblem> selectorForRemaining, int elites = 1)
      : base(selectorForRemaining)
    {
        this.elites = elites;
    }

    protected override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population,
                                                                  ObjectiveDirections objective, int count, InnerSelect innerSelect,
                                                                  IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        var selectedElites = BestSelector.Select(population, objective, elites);
        var remainingCount = count - selectedElites.Count;
        var selecterdRemaining = innerSelect(population, objective, remainingCount, random, searchSpace, problem);

        return selectedElites.Concat(selecterdRemaining).ToArray();
    }
}

// public static class EliteSelector
// {
//   public static EliteSelector<TCandidate, TSearchSpace, TProblem> WithElites<TCandidate, TSearchSpace, TProblem>(this ISelector<TCandidate, TSearchSpace, TProblem> selector, int elites = 1) where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(selector, elites);
// }
