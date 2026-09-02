using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <inheritdoc cref="GeneticAlgorithmDefaults" path="/summary"/>
/// <remarks><inheritdoc cref="GeneticAlgorithmDefaults" path="/remarks/node()"/></remarks>
public static class HillClimberDefaults
{
    public const int MaxNeighbors = 100;

    public const int BatchSize = 100;

    public const LocalSearchDirection Direction = LocalSearchDirection.FirstImprovement;

    public static IEvaluator<TCandidate> Evaluator<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new ProblemEvaluator<TCandidate>();
}
