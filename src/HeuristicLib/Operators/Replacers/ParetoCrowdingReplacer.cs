using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record ParetoCrowdingReplacer<TCandidate>
    : StatelessReplacer<TCandidate>
{
    public bool DominateOnEqualities { get; init; }

    public ParetoCrowdingReplacer(bool dominateOnEqualities)
    {
        DominateOnEqualities = dominateOnEqualities;
    }

    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        ParetoCrowdingReplacer.Replace(previousPopulation, offspringPopulation, objective, count, DominateOnEqualities);
}

public static class ParetoCrowdingReplacer
{
    public static ParetoCrowdingReplacer<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, bool dominateOnEqualities) => new(dominateOnEqualities);

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace<TCandidate>(
        IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation,
        IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
        ObjectiveDirections objective,
        int count,
        bool dominateOnEqualities)
    {
        var all = previousPopulation.Concat(offspringPopulation).ToArray();
        var fronts = DominationCalculator.CalculateAllParetoFronts(all, objective, out _, dominateOnEqualities);

        var l = new List<EvaluatedCandidate<TCandidate>>();
        var size = count;
        foreach (var front in fronts)
        {
            if (front.Count < size)
            {
                l.AddRange(front);
                size -= front.Count;

                continue;
            }

            var dist = CrowdingDistance.CalculateCrowdingDistances(front.Select(x => x.ObjectiveVector).ToList());
            l.AddRange(front.Select((x, i) => (x, i)).OrderByDescending(x => dist[x.i]).Select(x => x.x).Take(size));

            break;
        }

        return l;
    }
}
