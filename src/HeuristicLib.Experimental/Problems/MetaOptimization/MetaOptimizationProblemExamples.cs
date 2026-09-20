using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.MetaOptimization;

public static class MetaOptimizationProblemExamples
{
    public record HyperParameterSearchSpace(BoundedRealVectorSearchSpace SearchSpace, IntegerVectorSearchSpace SearchSpace2) :
        CompositeSearchSpace<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(SearchSpace, SearchSpace2);

    public class MetaOptimizationSearchSpaceBuilder
    {
        public readonly List<int> IntegerMinimum = [];
        public readonly List<int> IntegerMaximum = [];
        public readonly List<double> RealMinimum = [];
        public readonly List<double> RealMaximum = [];

        public HyperParameterSearchSpace Build() => new HyperParameterSearchSpace(
            new BoundedRealVectorSearchSpace(RealMinimum.Count, new RealVector(RealMinimum), new RealVector(RealMaximum)),
            new IntegerVectorSearchSpace(IntegerMinimum.Count, new IntegerVector(IntegerMinimum), new IntegerVector(IntegerMaximum)));

        public Func<CompositeGenotype<RealVector, IntegerVector>, TCandidate> AddChoiceParameter<TCandidate>(IReadOnlyList<TCandidate> values)
        {
            var n = IntegerMinimum.Count;
            IntegerMinimum.Add(0);
            IntegerMaximum.Add(values.Count - 1);
            return x => values[x.Part2[n]];
        }

        public Func<CompositeGenotype<RealVector, IntegerVector>, TCandidate> AddChoiceParameter<TCandidate>(params IEnumerable<TCandidate> values) => AddChoiceParameter(values.ToArray());

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

    public static MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> GeneticAlgorithmMetaOptimizationProblem<TCandidate, TSearchSpace, TProblem>(
      this TProblem problem,
      ICreator<TCandidate>[] creators,
      ICrossover<TCandidate>[] crossovers,
      IEvaluator<TCandidate>[] evaluators,
      IInterceptor<TCandidate>[] interceptors,
      IMutator<TCandidate>[] mutators,
      (int min, int max) elites,
      ISelector<TCandidate>[] selectors,
      (int min, int max) populationSize,
      (double min, double max) mutationRate)
        where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace> where TCandidate : class
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
        return new MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(problem, combinedSearchSpace, x => new GeneticAlgorithm<TCandidate>
        {
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

    public static MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>> EvolutionStrategyMetaOptimizationProblem<TCandidate, TSearchSpace, TProblem>(
      this TProblem problem,
      ICreator<TCandidate>[] creators,
      ICrossover<TCandidate>[] crossovers,
      IEvaluator<TCandidate>[] evaluators,
      IInterceptor<TCandidate>[] interceptors,
      IMutator<TCandidate>[] mutators,
      EvolutionStrategyType[] strategies,
      IReplacer<TCandidate>[] replacers,
      ISelector<TCandidate>[] selectors,
      (int min, int max) populationSize,
      (int min, int max) numberOfChildren,
      (double min, double max) mutationRate)
        where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace> where TCandidate : class
    {
        IntegerVector integerMins = [0, 0, 0, 0, 0, 0, 0, populationSize.min, numberOfChildren.min];
        IntegerVector integerMaxs =
        [
            creators.Length - 1,
            crossovers.Length - 1,
            evaluators.Length - 1,
            interceptors.Length - 1,
            mutators.Length - 1,
            strategies.Length - 1,
            replacers.Length - 1,
            selectors.Length - 1,
            populationSize.max,
            numberOfChildren.max
        ];
        var integerVectorSearchSpace = new IntegerVectorSearchSpace(integerMins.Count, integerMins, integerMaxs);
        var realVectorSearchSpace = new BoundedRealVectorSearchSpace(1, mutationRate.min, mutationRate.max);
        var combinedSearchSpace = realVectorSearchSpace.CombinedWith<RealVector, BoundedRealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>(integerVectorSearchSpace);

        return new MetaOptimizationProblem<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>(problem, combinedSearchSpace, AlgBuilder);

        IAlgorithm<TCandidate> AlgBuilder(CompositeGenotype<RealVector, IntegerVector> x)
        {
            var ints = x.Part2;
            return new EvolutionStrategy<TCandidate>
            {
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
