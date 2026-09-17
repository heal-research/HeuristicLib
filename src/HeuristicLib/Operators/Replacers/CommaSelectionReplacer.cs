using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record CommaSelectionReplacer<TCandidate>
    : StatelessReplacer<TCandidate>
{
    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        CommaSelectionReplacer.Replace(offspringPopulation, objective, count);
}

public static class CommaSelectionReplacer
{
    public static CommaSelectionReplacer<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => new();

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count) =>
        offspringPopulation
            .OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer)
            .Take(count)
            .ToArray();
}
