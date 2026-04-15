using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Problems.MetaOptimization;

public static class MetaOptimizationProblemExamples
{
  public record HyperParameterSearchSpace(RealVectorSearchSpace SearchSpace, IntegerVectorSearchSpace SearchSpace2) :
    CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(SearchSpace, SearchSpace2);

  public class MetaOptimizationSearchSpaceBuilder
  {
    public readonly List<int> IntegerMinimum = [];
    public readonly List<int> IntegerMaximum = [];
    public readonly List<double> RealMinimum = [];
    public readonly List<double> RealMaximum = [];

    public HyperParameterSearchSpace Build() => new HyperParameterSearchSpace(
      new RealVectorSearchSpace(RealMinimum.Count, new RealVector(RealMinimum), new RealVector(RealMaximum)),
      new IntegerVectorSearchSpace(IntegerMinimum.Count, new IntegerVector(IntegerMinimum), new IntegerVector(IntegerMaximum)));

    public Func<CompositeGenotype<RealVector, IntegerVector>, T> AddChoiceParameter<T>(IReadOnlyList<T> values)
    {
      var n = IntegerMinimum.Count;
      IntegerMinimum.Add(0);
      IntegerMaximum.Add(values.Count - 1);
      return x => values[x.Part2[n]];
    }

    public Func<CompositeGenotype<RealVector, IntegerVector>, T> AddChoiceParameter<T>(params IEnumerable<T> values) => AddChoiceParameter(values.ToArray());

    public Func<CompositeGenotype<RealVector, IntegerVector>, int> AddIntegerParameter(int min, int max)
    {
      var n = IntegerMinimum.Count;
      IntegerMinimum.Add(min);
      IntegerMaximum.Add(max);
      return x => x.Part2[n];
    }

    public Func<CompositeGenotype<RealVector, IntegerVector>, int> AddIntegerParameter((int min, int max) bounds) => AddIntegerParameter(bounds.min, bounds.max);

    public Func<CompositeGenotype<RealVector, IntegerVector>, double> AddRealParameter(double min, double max)
    {
      int n = RealMinimum.Count;
      RealMinimum.Add(min);
      RealMaximum.Add(max);
      return x => x.Part1[n];
    }

    public Func<CompositeGenotype<RealVector, IntegerVector>, double> AddRealParameter((double min, double max) bounds) => AddRealParameter(bounds.min, bounds.max);
  }

  public static MetaOptimizationProblem<T, TE, TP, PopulationState<T>> GeneticAlgorithmMetaOptimizationProblem<T, TE, TP>(
    this TP problem,
    ICreator<T, TE, TP>[] creators,
    ICrossover<T, TE, TP>[] crossovers,
    IEvaluator<T, TE, TP>[] evaluators,
    IInterceptor<T, TE, TP, PopulationState<T>>[] interceptors,
    IMutator<T, TE, TP>[] mutators,
    (int min, int max) elites,
    ISelector<T, TE, TP>[] selectors,
    (int min, int max) populationSize,
    (double min, double max) mutationRate)
    where TE : class, ISearchSpace<T> where TP : class, IProblem<T, TE> where T : class
  {
    var b = new MetaOptimizationSearchSpaceBuilder();
    var creatorExtractor = b.AddChoiceParameter(creators);
    var crossoversExtractor = b.AddChoiceParameter(crossovers);
    var evaluatorsExtractor = b.AddChoiceParameter(evaluators);
    var interceptorsExtractor = b.AddChoiceParameter(interceptors);
    var mutatorsExtractor = b.AddChoiceParameter(mutators);
    var elitesExtractor = b.AddIntegerParameter(elites);
    var selectorsExtractor = b.AddChoiceParameter(selectors);
    var popSizeExtractor = b.AddIntegerParameter(populationSize);
    var rateExtractor = b.AddRealParameter(mutationRate);
    var combinedSearchSpace = b.Build();
    return new MetaOptimizationProblem<T, TE, TP, PopulationState<T>>(problem, combinedSearchSpace, x => new GeneticAlgorithm<T, TE, TP> {
      Creator = creatorExtractor(x),
      Crossover = crossoversExtractor(x),
      Evaluator = evaluatorsExtractor(x),
      Interceptor = interceptorsExtractor(x),
      Mutator = mutatorsExtractor(x),
      Selector = selectorsExtractor(x),
      PopulationSize = popSizeExtractor(x),
      MutationRate = rateExtractor(x),
      Elites = elitesExtractor(x)
    });
  }

  public static MetaOptimizationProblem<T, TE, TP, PopulationState<T>> EvolutionStrategyMetaOptimizationProblem<T, TE, TP>(
    this TP problem,
    ICreator<T, TE, TP>[] creators,
    ICrossover<T, TE, TP>[] crossovers,
    IEvaluator<T, TE, TP>[] evaluators,
    IInterceptor<T, TE, TP, PopulationState<T>>[] interceptors,
    IMutator<T, TE, TP>[] mutators,
    EvolutionStrategyType[] strategies,
    IReplacer<T, TE, TP>[] replacers,
    ISelector<T, TE, TP>[] selectors,
    (int min, int max) populationSize,
    (int min, int max) numberOfChildren,
    (double min, double max) mutationRate)
    where TE : class, ISearchSpace<T> where TP : class, IProblem<T, TE> where T : class
  {
    var integerMins = new IntegerVector(0, 0, 0, 0, 0, 0, 0, populationSize.min, numberOfChildren.min);
    var integerMaxs = new IntegerVector(
      creators.Length - 1,
      crossovers.Length - 1,
      evaluators.Length - 1,
      interceptors.Length - 1,
      mutators.Length - 1,
      strategies.Length - 1,
      replacers.Length - 1,
      selectors.Length - 1,
      populationSize.max,
      numberOfChildren.max);
    var integerVectorSearchSpace = new IntegerVectorSearchSpace(integerMins.Count, integerMins, integerMaxs);
    var realVectorSearchSpace = new RealVectorSearchSpace(1, mutationRate.min, mutationRate.max);
    var combinedSearchSpace = realVectorSearchSpace.WithSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(integerVectorSearchSpace);

    return new MetaOptimizationProblem<T, TE, TP, PopulationState<T>>(problem, combinedSearchSpace, AlgBuilder);

    IAlgorithm<T, TE, TP, PopulationState<T>> AlgBuilder(CompositeGenotype<RealVector, IntegerVector> x)
    {
      var ints = x.Part2;
      return new EvolutionStrategy<T, TE, TP> {
        Creator = creators[ints[0]],
        Crossover = crossovers[ints[1]],
        Evaluator = evaluators[ints[2]],
        Interceptor = interceptors[ints[3]],
        Mutator = mutators[ints[4]],
        Strategy = strategies[ints[5]],
        Selector = selectors[ints[6]],
        PopulationSize = ints[7],
        NumberOfChildren = ints[8],
      };
    }
  }
}
