using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

/// <inheritdoc cref="GeneticAlgorithmDefaults" path="/summary"/>
/// <remarks><inheritdoc cref="GeneticAlgorithmDefaults" path="/remarks/node()"/></remarks>
public static class EvolutionStrategyDefaults
{
    public const int PopulationSize = 100;

    public const int NumberOfChildren = 100;

    public const EvolutionStrategyType Strategy = EvolutionStrategyType.Plus;

    public static ISelector<TCandidate, TSearchSpace, TProblem> Selector<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new RandomSelector<TCandidate>();

    public static IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new ProblemEvaluator<TCandidate, TSearchSpace, TProblem>();
}
