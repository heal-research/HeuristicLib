using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Replacers;

public record PlusSelectionReplacer<TCandidate>
  : StatelessReplacer<TCandidate>
{
    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
    {
        return Replace(previousPopulation, offspringPopulation, objective, count);
    }

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(
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
