using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

public record WorstSelector<TCandidate>
    : StatelessSelector<TCandidate>
{
    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        WorstSelector.Select(population, objective, count);
}

public static class WorstSelector
{
    public static WorstSelector<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new();

    public static IReadOnlyList<int> Select(IReadOnlyList<ObjectiveVector> population, ObjectiveDirections objective, int count = 1) =>
        population.Select((solution, index) => (solution, index)).OrderByDescending(x => x.solution, objective.TotalOrderComparer).Take(count).Select(x => x.index).ToList();

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count) =>
        population.OrderByDescending(x => x.ObjectiveVector, objective.TotalOrderComparer).Take(count).ToList();
}
