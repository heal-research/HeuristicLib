using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Algorithms.NSGA2;

namespace HEAL.HeuristicLib.Algorithms;

public static class AlgorithmFactory {
  public static GeneticAlgorithm<TGenotype, TEncoding, TProblem> GeneticAlgorithm<TGenotype, TEncoding, TProblem>(
    int populationSize,
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator,
    double mutationRate,
    ISelector<TGenotype, TEncoding, TProblem> selector,
    IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
    int elites,
    int? randomSeed,
    ITerminator<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IInterceptor<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null,
    params IAnalyzer<TGenotype, PopulationIterationResult<TGenotype>, TEncoding, TProblem>[] analyzers)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding> {
    return new GeneticAlgorithm<TGenotype, TEncoding, TProblem>(populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, elites, randomSeed, terminator, MultiInterceptor(interceptor, analyzers));
  }

  public static MultiInterceptor<TGenotype, TResult, TEncoding, TProblem>? MultiInterceptor<TGenotype, TResult, TEncoding, TProblem>(
    IInterceptor<TGenotype, TResult, TEncoding, TProblem>? interceptor,
    IAnalyzer<TGenotype, TResult, TEncoding, TProblem>[] analyzers)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding>
    where TResult : IIterationResult {
    var list = new List<IInterceptor<TGenotype, TResult, TEncoding, TProblem>>();
    if (interceptor != null)
      list.Add(interceptor);
    if (analyzers.Length != 0)
      list.Add(new AnalysisInterceptor<TGenotype, TResult, TEncoding, TProblem>(analyzers));
    return list.Count == 0 ? null : new MultiInterceptor<TGenotype, TResult, TEncoding, TProblem>(list);
  }

  public static LocalSearch<TGenotype, TEncoding, TProblem> LocalSearch<TGenotype, TEncoding, TProblem>(
    ICreator<TGenotype, TEncoding, TProblem> creator,
    IMutator<TGenotype, TEncoding, TProblem> mutator,
    ITerminator<TGenotype, SingleSolutionIterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
    int? randomSeed,
    int maxNeighbors,
    int batchSize,
    LocalSearchDirection direction,
    IInterceptor<TGenotype, SingleSolutionIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null,
    params IAnalyzer<TGenotype, SingleSolutionIterationResult<TGenotype>, TEncoding, TProblem>[] analyzers)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding>
    => new(terminator, MultiInterceptor(interceptor, analyzers), creator, mutator, evaluator, randomSeed, maxNeighbors, batchSize, direction);

  public static NSGA2<TGenotype, TEncoding, TProblem> NSGA2<TGenotype, TEncoding, TProblem>(
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator,
    ISelector<TGenotype, TEncoding, TProblem> selector,
    ITerminator<TGenotype, NSGA2IterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
    int? randomSeed,
    int populationSize,
    double mutationRate,
    bool dominateOnEquals,
    IInterceptor<TGenotype, NSGA2IterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null,
    params IAnalyzer<TGenotype, NSGA2IterationResult<TGenotype>, TEncoding, TProblem>[] analyzers)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding>
    => new(terminator, MultiInterceptor(interceptor, analyzers), populationSize, creator, crossover, mutator, mutationRate, selector, evaluator, randomSeed, dominateOnEquals);
}
