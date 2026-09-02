using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <inheritdoc cref="GeneticAlgorithmDefaults" path="/summary"/>
/// <remarks><inheritdoc cref="GeneticAlgorithmDefaults" path="/remarks/node()"/></remarks>
#pragma warning disable S101
public static class NSGA2Defaults
#pragma warning restore S101
{
    public const int PopulationSize = 100;

    public const double MutationRate = GeneticAlgorithmDefaults.MutationRate;

    /// <summary>
    /// Whether candidates of equal objective vectors dominate one another. Selection and replacement must agree on
    /// this, so both defaults read it.
    /// </summary>
    public const bool DominateOnEquals = true;

    public static ISelector<TCandidate> Selector<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new ParetoCrowdingTournamentSelector<TCandidate>(DominateOnEquals);

    public static IReplacer<TCandidate> Replacer<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new ParetoCrowdingReplacer<TCandidate>(DominateOnEquals);

    public static IEvaluator<TCandidate> Evaluator<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new ProblemEvaluator<TCandidate>();
}
