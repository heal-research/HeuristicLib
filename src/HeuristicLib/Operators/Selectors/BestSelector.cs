using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record BestSelector<TCandidate>
    : StatelessSelector<TCandidate>
{
    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        BestSelector.Select(population, objective, count);
}

public static class BestSelector
{
    public static BestSelector<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => new();

    public static IReadOnlyList<int> Select(IReadOnlyList<ObjectiveVector> population, ObjectiveDirections objective, int count = 1) =>
        population.Select((solution, index) => (solution, index)).OrderBy(x => x.solution, objective.TotalOrderComparer).Take(count).Select(x => x.index).ToList();

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count) =>
        population.OrderBy(x => x.ObjectiveVector, objective.TotalOrderComparer).Take(count).ToList();
}
