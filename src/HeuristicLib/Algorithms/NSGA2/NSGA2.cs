// using System.Diagnostics;
// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Random;
//
// namespace HEAL.HeuristicLib.Algorithms.NSGA2;
//
// public record class NSGA2<TGenotype, TProblem>
//   : IterativeAlgorithm<TGenotype, TProblem, NSGA2State<TGenotype>, NSGA2IterationResult<TGenotype>, NSGA2Result<TGenotype>>
//   where TProblem : IOptimizable<TGenotype>
// {
//   public int PopulationSize { get; init; }
//   public Creator<TGenotype, TProblem> Creator { get; init; }
//   public Crossover<TGenotype, TProblem> Crossover { get; init; }
//   public Mutator<TGenotype, TProblem> Mutator { get; init; }
//
//   public double MutationRate { get; init; }
//
//   //public ISelector Selector { get; init; }
//   public Replacer<TProblem> Replacer { get; init; }
//   public int RandomSeed { get; init; }
//   public Interceptor<TProblem, NSGA2IterationResult<TGenotype>>? Interceptor { get; init; }
//
//   public NSGA2(
//     int populationSize,
//     Creator<TGenotype, TProblem> creator,
//     Crossover<TGenotype, TProblem> crossover,
//     Mutator<TGenotype, TProblem> mutator, double mutationRate,
//     /*ISelector selector,*/ Replacer<TProblem> replacer,
//     int randomSeed,
//     Terminator<TProblem, NSGA2Result<TGenotype>> terminator,
//     Interceptor<TProblem, NSGA2IterationResult<TGenotype>>? interceptor = null
//   ) : base(terminator) {
//     PopulationSize = populationSize;
//     Creator = creator;
//     Crossover = crossover;
//     Mutator = mutator;
//     MutationRate = mutationRate;
//     //Selector = selector;
//     Replacer = replacer;
//     RandomSeed = randomSeed;
//     Interceptor = interceptor;
//   }
//
//   public override Nsga2Execution<TGenotype, TProblem> CreateStreamingExecution(TProblem problem) {
//     return new Nsga2Execution<TGenotype, TProblem>(this, problem);
//   }
// }
//
// // public record class NSGA2<TGenotype, TSearchSpace>
// //   : NSGA2<TGenotype, TSearchSpace, IOptimizable<TGenotype, TSearchSpace>>
// //   where TSearchSpace : ISearchSpace<TGenotype>
// // {
// //   public NSGA2(
// //     int populationSize,
// //     Creator<TGenotype, TSearchSpace> creator,
// //     Crossover<TGenotype, TSearchSpace> crossover,
// //     Mutator<TGenotype, TSearchSpace> mutator, double mutationRate,
// //     /*ISelector selector,*/ ComplexReplacer<TGenotype, TSearchSpace> replacer,
// //     int randomSeed,
// //     Terminator<TGenotype, TSearchSpace, NSGA2Result<TGenotype>> terminator,
// //     Interceptor<TGenotype, TSearchSpace, IOptimizable<TGenotype, TSearchSpace>, NSGA2IterationResult<TGenotype>>? interceptor = null
// //   ) : base(populationSize, creator, crossover, mutator, mutationRate, replacer, randomSeed, terminator, interceptor) { }
// // }
//
// public class Nsga2Execution<TGenotype, TProblem>
//   : IterativeAlgorithmExecution<TGenotype, TProblem, NSGA2State<TGenotype>, NSGA2IterationResult<TGenotype>, NSGA2Result<TGenotype>, NSGA2<TGenotype, TProblem>>
//   where TProblem : IOptimizable<TGenotype>
// {
//   public IRandomNumberGenerator Random { get; }
//   public ICreatorExecution<TGenotype> Creator { get; }
//   public ICrossoverExecution<TGenotype> Crossover { get; }
//   public IMutatorExecution<TGenotype> Mutator { get; }
//   public IReplacerExecution Replacer { get; }
//   public IInterceptorExecution<NSGA2IterationResult<TGenotype>>? Interceptor { get; }
//   
//   public Nsga2Execution(NSGA2<TGenotype, TProblem> parameters, TProblem problem) : base (parameters, problem) {
//     Random = new SystemRandomNumberGenerator(parameters.RandomSeed);
//     Creator = parameters.Creator.CreateExecution(problem);
//     Crossover = parameters.Crossover.CreateExecution(problem);
//     Mutator = parameters.Mutator.CreateExecution(problem);
//     Replacer = parameters.Replacer.CreateExecution(problem);
//     Interceptor = parameters.Interceptor?.CreateExecution(problem);
//   }
//
//   // public override NSGA2Result<TGenotype> Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, NSGA2State<TGenotype>? initialState = null) {
//   //   IReadOnlyList<EvaluatedIndividual<TGenotype>> paretoFront = [];
//   //   
//   //   int totalGenerations = 0;
//   //   TimeSpan totalDuration = TimeSpan.Zero;
//   //   var totalMetrics = new NSGA2OperatorMetrics();
//   //   
//   //   foreach (var result in ExecuteStreaming(problem, initialState)) {
//   //     paretoFront = Population.ExtractParetoFront(paretoFront.Concat(result.ParetoFront).ToList(), problem.Objective);
//   //     totalGenerations += 1;
//   //     totalDuration += result.TotalDuration;
//   //     totalMetrics += result.OperatorMetrics;
//   //   }
//   //   
//   //   return new NSGA2Result<TGenotype> {
//   //     TotalGenerations = totalGenerations,
//   //     TotalDuration = totalDuration,
//   //     OperatorMetrics = totalMetrics,
//   //     CurrentParetoFront = paretoFront
//   //   };
//   // }
//   
//   protected override NSGA2IterationResult<TGenotype> ExecuteInitialization() {
//     var start = Stopwatch.GetTimestamp();
//
//     // var random = new SystemRandomNumberGenerator(Parameters.RandomSeed);
//
//     var startCreating = Stopwatch.GetTimestamp();
//     var genotypes = Enumerable.Range(0, Parameters.PopulationSize).Select(i => Creator.Create(Random)).ToArray();
//     var endCreating = Stopwatch.GetTimestamp();
//     
//     // var genotypePopulation = newPopulation;
//     
//     // var startDecoding = Stopwatch.GetTimestamp();
//     // var phenotypePopulation = genotypePopulation.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
//     // var endDecoding = Stopwatch.GetTimestamp();
//     
//     var startEvaluating = Stopwatch.GetTimestamp();
//     var fitnesses = genotypes.Select(genotype => Problem.Evaluate(genotype)).ToArray();
//     var endEvaluating = Stopwatch.GetTimestamp();
//
//     var population = Population.From(genotypes, /*phenotypePopulation,*/ fitnesses);
//     
//     var endBeforeInterceptor = Stopwatch.GetTimestamp();
//     
//     var result = new NSGA2IterationResult<TGenotype>() {
//       Generation = 0,
//       Objective = Problem.Objective,
//       Population = population,
//       Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
//       OperatorMetrics = new NSGA2OperatorMetrics() {
//         Creation = new OperatorMetric(genotypes.Length, Stopwatch.GetElapsedTime(startCreating, endCreating)),
//         // Decoding = new OperatorMetric(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
//         Evaluation = new OperatorMetric(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating)),
//       }
//     };
//     
//     if (Interceptor is null) return result;
//     
//     var interceptorStart = Stopwatch.GetTimestamp();
//     var interceptedResult = Interceptor.Transform(result);
//     var interceptorEnd = Stopwatch.GetTimestamp();
//     
//     if (interceptedResult == result) return result;
//
//     var end = Stopwatch.GetTimestamp();
//     return interceptedResult with {
//       Duration = Stopwatch.GetElapsedTime(start, end),
//       OperatorMetrics = interceptedResult.OperatorMetrics with {
//         Interception = new OperatorMetric(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
//       }
//     };
//   }
//
//   protected override NSGA2IterationResult<TGenotype> ExecuteIteration(NSGA2State<TGenotype> state) {
//     var start = Stopwatch.GetTimestamp();
//     
//     // int newRandomSeed = SeedSequence.GetSeed(Algorithm.RandomSeed, state.Generation);
//     // var random = new SystemRandomNumberGenerator(newRandomSeed);
//     
//     int offspringCount = Replacer.GetOffspringCount(Parameters.PopulationSize);
//
//     var oldPopulation = state.Population;
//     
//     var startSelection = Stopwatch.GetTimestamp();
//     var selector = new RandomSelector().CreateExecution(); // ToDo: implement NSGA-specific selection (pareto-based selection)
//     var parents = selector.Select(oldPopulation, Problem.Objective, offspringCount * 2, Random).ToList();
//     var endSelection = Stopwatch.GetTimestamp();
//      
//     var genotypes = new TGenotype[offspringCount];
//     var startCrossover = Stopwatch.GetTimestamp();
//     int crossoverCount = 0;
//     for (int i = 0; i < parents.Count; i += 2) {
//       var parent1 = parents[i];
//       var parent2 = parents[i + 1];
//       var child = Crossover.Cross(parent1.Genotype, parent2.Genotype, Random);
//       genotypes[i / 2] = child;
//       crossoverCount++;
//     }
//     var endCrossover = Stopwatch.GetTimestamp();
//     
//     var startMutation = Stopwatch.GetTimestamp();
//     int mutationCount = 0;
//     for (int i = 0; i < genotypes.Length; i++) {
//       if (Random.Random() < Parameters.MutationRate) {
//         genotypes[i] = Mutator.Mutate(genotypes[i], Random);
//         mutationCount++;
//       }
//     }
//     var endMutation = Stopwatch.GetTimestamp();
//     
//     // var startDecoding = Stopwatch.GetTimestamp();
//     // var phenotypePopulation = genotypes.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
//     // var endDecoding = Stopwatch.GetTimestamp();
//     
//     var startEvaluation = Stopwatch.GetTimestamp();
//     var fitnesses = genotypes.Select(genotype => Problem.Evaluate(genotype)).ToArray();
//     var endEvaluation = Stopwatch.GetTimestamp();
//     
//     var population = Population.From(genotypes, /*phenotypePopulation,*/ fitnesses);
//     
//     var startReplacement = Stopwatch.GetTimestamp();
//     var newPopulation = Replacer.Replace(oldPopulation, population, Problem.Objective, Random);
//     var endReplacement = Stopwatch.GetTimestamp();
//     
//     var endBeforeInterceptor = Stopwatch.GetTimestamp();
//     
//     var result = new NSGA2IterationResult<TGenotype>() {
//       Generation = state.Generation + 1,
//       Objective = Problem.Objective,
//       Population = newPopulation,
//       Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
//       OperatorMetrics = new NSGA2OperatorMetrics() {
//         Selection = new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection)),
//         Crossover = new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover)),
//         Mutation = new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation)),
//         // Decoding = new OperatorMetric(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
//         Evaluation = new OperatorMetric(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation)),
//         Replacement = new OperatorMetric(1, Stopwatch.GetElapsedTime(startReplacement, endReplacement))
//       }
//     };
//     
//     if (Interceptor is null) return result;
//     
//     var interceptorStart = Stopwatch.GetTimestamp();
//     var interceptedResult = Interceptor.Transform(result);
//     var interceptorEnd = Stopwatch.GetTimestamp();
//     if (interceptedResult == result) return result;
//     
//     var end = Stopwatch.GetTimestamp();
//
//     return interceptedResult with {
//       Duration = Stopwatch.GetElapsedTime(start, end),
//       OperatorMetrics = interceptedResult.OperatorMetrics with {
//         Interception = new OperatorMetric(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
//       }
//     };
//   }
//   protected override NSGA2Result<TGenotype> AggregateResult(NSGA2IterationResult<TGenotype> iterationResult, NSGA2Result<TGenotype>? algorithmResult) {
//     var currentParetoFront = ParetoFront.ExtractFrom(iterationResult.Population, iterationResult.Objective);
//     return new NSGA2Result<TGenotype>() {
//       CurrentGeneration = iterationResult.Generation,
//       TotalGenerations = iterationResult.Generation,
//       CurrentDuration = iterationResult.Duration,
//       TotalDuration = iterationResult.Duration + (algorithmResult?.TotalDuration ?? TimeSpan.Zero),
//       CurrentOperatorMetrics = iterationResult.OperatorMetrics,
//       TotalOperatorMetrics = iterationResult.OperatorMetrics + (algorithmResult?.TotalOperatorMetrics ?? new NSGA2OperatorMetrics()),
//       Objective = iterationResult.Objective,
//       CurrentPopulation = iterationResult.Population,
//       CurrentParetoFront = currentParetoFront,
//       ParetoFront = algorithmResult is null ? currentParetoFront : ParetoFront.ExtractFrom(algorithmResult.ParetoFront.Concat(currentParetoFront), iterationResult.Objective)
//     };
//   }
// }
//
//
// public record NSGA2State<TGenotype> : IAlgorithmState {
//   public required int Generation { get; init; }
//   public required IReadOnlyList<Solution<TGenotype>> Population { get; init; }
// }
//
// public record NSGA2OperatorMetrics {
//   public OperatorMetric Creation { get; init; } = OperatorMetric.Zero;
//   // public OperatorMetric Decoding { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Evaluation { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Selection { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Crossover { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Mutation { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Replacement { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Interception { get; init; } = OperatorMetric.Zero;
//   
//   public static NSGA2OperatorMetrics Aggregate(NSGA2OperatorMetrics left, NSGA2OperatorMetrics right) {
//     return new NSGA2OperatorMetrics {
//       Creation = left.Creation + right.Creation,
//       // Decoding = left.Decoding + right.Decoding,
//       Evaluation = left.Evaluation + right.Evaluation,
//       Selection = left.Selection + right.Selection,
//       Crossover = left.Crossover + right.Crossover,
//       Mutation = left.Mutation + right.Mutation,
//       Replacement = left.Replacement + right.Replacement,
//       Interception = left.Interception + right.Interception
//     };
//   }
//   public static NSGA2OperatorMetrics operator +(NSGA2OperatorMetrics left, NSGA2OperatorMetrics right) => Aggregate(left, right);
// }
//
// public record NSGA2IterationResult<TGenotype>  {
//   public required int Generation { get; init; }
//   public required TimeSpan Duration { get; init; }
//   public required NSGA2OperatorMetrics OperatorMetrics { get; init; }
//   public required Objective Objective { get; init; }
//   public required IReadOnlyList<Solution<TGenotype>> Population { get; init; }
// }
//
// public record NSGA2Result<TGenotype> : IMultiObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<NSGA2State<TGenotype>> {
//   int IAlgorithmResult.CurrentIteration => CurrentGeneration;
//   int IAlgorithmResult.TotalIterations => TotalGenerations;
//   
//   public required int CurrentGeneration { get; init; }
//   public required int TotalGenerations { get; init; }
//   
//   public required TimeSpan CurrentDuration { get; init; }
//   public required TimeSpan TotalDuration { get; init; }
//   
//   public required NSGA2OperatorMetrics CurrentOperatorMetrics { get; init; }
//   public required NSGA2OperatorMetrics TotalOperatorMetrics { get; init; }
//
//   public required Objective Objective { get; init; }
//   
//   public required IReadOnlyList<Solution<TGenotype>> CurrentPopulation { get; init; }
//   
//   // public NSGA2Result() {
//   //   currentParetoFront = new Lazy<IReadOnlyList<EvaluatedIndividual<TGenotype>>>(() => {
//   //     return Population.ExtractParetoFront(CurrentPopulation, Objective);
//   //   });
//   // }
//   
//   // private readonly Lazy<IReadOnlyList<EvaluatedIndividual<TGenotype>>> currentParetoFront;
//   // public IReadOnlyList<EvaluatedIndividual<TGenotype>> CurrentParetoFront => currentParetoFront.Value;
//   public required IReadOnlyList<Solution<TGenotype>> CurrentParetoFront { get; init; }
//   public required IReadOnlyList<Solution<TGenotype>> ParetoFront { get; init; }
//
//   public NSGA2State<TGenotype> GetContinuationState() => new() {
//     Generation = CurrentGeneration,
//     Population = CurrentPopulation
//   };
//
//   public NSGA2State<TGenotype> GetRestartState() => GetContinuationState() with { Generation = 0 };
// }


