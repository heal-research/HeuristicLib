using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

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
    public static PlusSelectionReplacer<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new();

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
