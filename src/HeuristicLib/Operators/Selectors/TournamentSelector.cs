using HEAL.HeuristicLib.Execution;
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

    public static Parameter<TournamentSelector<TCandidate>, int> TournamentSizeParameter { get; } =
        new(nameof(TournamentSize), static selector => selector.TournamentSize, static (selector, value) => selector with { TournamentSize = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [TournamentSizeParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public TournamentSelector(int tournamentSize)
    {
        TournamentSize = tournamentSize;
    }

    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        TournamentSelector.Select(population, objective, count, random, TournamentSize);
}

public static class TournamentSelector
{
    public static TournamentSelector<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, int tournamentSize) => new(tournamentSize);


    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Select<TCandidate>(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, int tournamentSize)
    {
        return Enumerable
            .Range(0, count)
            .Select(_ => random
                .NextInts(tournamentSize, population.Count)
                .Select(i1 => population[i1])
                .MinBy(participant => participant.ObjectiveVector, objective.TotalOrderComparer)!
            )
            .ToArray();
    }
}
