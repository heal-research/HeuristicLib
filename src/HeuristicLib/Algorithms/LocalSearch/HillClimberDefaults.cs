using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;

/// <inheritdoc cref="GeneticAlgorithmDefaults" path="/summary"/>
/// <remarks><inheritdoc cref="GeneticAlgorithmDefaults" path="/remarks/node()"/></remarks>
public static class HillClimberDefaults
{
    public const int MaxNeighbors = 100;

    public const int BatchSize = 100;

    public const LocalSearchDirection Direction = LocalSearchDirection.FirstImprovement;

    public static IEvaluator<TCandidate> Evaluator<TCandidate>() =>
        new ProblemEvaluator<TCandidate>();
}
