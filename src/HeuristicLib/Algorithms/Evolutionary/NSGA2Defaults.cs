using HEAL.HeuristicLib.Operators;

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

    public static ISelector<TCandidate> Selector<TCandidate>() =>
        new ParetoCrowdingTournamentSelector<TCandidate>(DominateOnEquals);

    public static IReplacer<TCandidate> Replacer<TCandidate>() =>
        new ParetoCrowdingReplacer<TCandidate>(DominateOnEquals);

    public static IEvaluator<TCandidate> Evaluator<TCandidate>() =>
        new ProblemEvaluator<TCandidate>();
}
