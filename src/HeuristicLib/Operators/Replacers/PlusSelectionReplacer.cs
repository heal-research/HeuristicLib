using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record PlusSelectionReplacer<TCandidate>
    : StatelessReplacer<TCandidate>
{
    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
    {
        return PlusSelectionReplacer.Replace(previousPopulation, offspringPopulation, objective, count);
    }
}

public static class PlusSelectionReplacer
{
    public static PlusSelectionReplacer<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => new();

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace<TCandidate>(
        IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
        IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
        ObjectiveDirections objective,
        int count)
    {
        var combinedPopulation = previousPopulation.Concat(offspringPopulation).ToList();
        return combinedPopulation
               .OrderBy(p => p.ObjectiveVector, objective.TotalOrderComparer)
               .Take(count)
               .ToArray();
    }
}
