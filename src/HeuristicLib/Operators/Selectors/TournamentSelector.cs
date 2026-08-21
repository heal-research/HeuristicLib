using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public record TournamentSelector<TCandidate>
    : StatelessSelector<TCandidate>
{
    public int TournamentSize { get; init; }

    public TournamentSelector(int tournamentSize)
    {
        TournamentSize = tournamentSize;
    }

    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        TournamentSelector.Select(population, objective, count, random, TournamentSize);
}

public static class TournamentSelector
{
    public static TournamentSelector<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, int tournamentSize)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(tournamentSize);

    public static TournamentSelector<TCandidate> For<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, int tournamentSize)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState => new(tournamentSize);

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, int tournamentSize)
    {
        return Enumerable
               .Range(0, count)
               .Select(_ => random.NextInts(tournamentSize, population.Count)
                                  .Select(i1 => population[i1])
                                  .MinBy(participant => participant.ObjectiveVector, objective.TotalOrderComparer)!)
               .ToArray();
    }
}
