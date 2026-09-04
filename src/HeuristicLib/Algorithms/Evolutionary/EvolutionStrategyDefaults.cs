using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;

/// <inheritdoc cref="GeneticAlgorithmDefaults" path="/summary"/>
/// <remarks><inheritdoc cref="GeneticAlgorithmDefaults" path="/remarks/node()"/></remarks>
public static class EvolutionStrategyDefaults
{
    public const int PopulationSize = 100;

    /// <inheritdoc cref="GeneticAlgorithmDefaults.MaximumGenerations"/>
    public const int MaximumGenerations = GeneticAlgorithmDefaults.MaximumGenerations;

    public const int NumberOfChildren = 100;

    public const EvolutionStrategyType Strategy = EvolutionStrategyType.Plus;

    public static ISelector<TCandidate> Selector<TCandidate>() =>
        new RandomSelector<TCandidate>();

    public static IEvaluator<TCandidate> Evaluator<TCandidate>() =>
        new ProblemEvaluator<TCandidate>();
}
