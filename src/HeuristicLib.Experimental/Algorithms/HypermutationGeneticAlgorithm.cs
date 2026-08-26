using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.Dynamic.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>
/// Convenience factory for genetic algorithms that use epoch-triggered hypermutation.
/// </summary>
/// <remarks>
/// References: Cobb, H. G. (1990). <i>An investigation into the use of hypermutation as an adaptive operator in
/// genetic algorithms having continuous, time-dependent nonstationary environments</i> (NRL-MR-6760). Naval
/// Research Laboratory. Cobb, H. G., &amp; Grefenstette, J. J. (1993). Genetic algorithms for tracking changing
/// environments. In S. Forrest (Ed.), <i>Proceedings of the Fifth International Conference on Genetic Algorithms</i>
/// (pp. 523-530). Morgan Kaufmann.
/// </remarks>
public static class HypermutationGeneticAlgorithm
{
    public static GeneticAlgorithm<TCandidate> Create<TCandidate, TSearchSpace, TProblem>(
        ICreator<TCandidate> creator,
        ICrossover<TCandidate> crossover,
        IMutator<TCandidate> mutator,
        double baseMutationRate,
        double hypermutationRate,
        int hypermutationDuration,
        ISelector<TCandidate> selector,
        int populationSize,
        IEvaluator<TCandidate> evaluator,
        int elites = 1,
        IInterceptor<TCandidate>? interceptor = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        return GeneticAlgorithm.Create(
            creator,
            crossover,
            new DynamicRateMutator<TCandidate, TSearchSpace, TProblem>(mutator) { BaseRate = baseMutationRate, ResponseRate = hypermutationRate, ResponseDuration = hypermutationDuration },
            selector: selector,
            populationSize: populationSize,
            mutationRate: 1.0,
            evaluator: evaluator,
            elites: elites,
            interceptor: interceptor);
    }
}
