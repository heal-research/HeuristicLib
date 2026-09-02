using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>
/// The starting points a genetic algorithm uses for settings and operators the caller does not supply.
/// </summary>
/// <remarks>
/// These are the single source for every construction path, so an omitted value means the same thing wherever it is
/// omitted. They are suggested starting points rather than tuned values: an algorithm that took a default behaves
/// differently if the default changes, so record which values were defaulted when a result has to be reproducible.
/// </remarks>
public static class GeneticAlgorithmDefaults
{
    public const int PopulationSize = 100;

    public const double MutationRate = 0.1;

    public const int Elites = 1;

    public const int TournamentSize = 2;

    public static ISelector<TCandidate> Selector<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new TournamentSelector<TCandidate>(TournamentSize);

    public static IEvaluator<TCandidate> Evaluator<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new ProblemEvaluator<TCandidate>();
}
