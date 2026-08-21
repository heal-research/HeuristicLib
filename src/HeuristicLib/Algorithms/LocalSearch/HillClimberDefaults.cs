using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

/// <inheritdoc cref="GeneticAlgorithmDefaults" path="/summary"/>
/// <remarks><inheritdoc cref="GeneticAlgorithmDefaults" path="/remarks/node()"/></remarks>
public static class HillClimberDefaults
{
    public const int MaxNeighbors = 100;

    public const int BatchSize = 100;

    public const LocalSearchDirection Direction = LocalSearchDirection.FirstImprovement;

    public static IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new ProblemEvaluator<TCandidate, TSearchSpace, TProblem>();
}
