// using System.Diagnostics;
// using HEAL.HeuristicLib.Genotypes;
// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Random;
//
// namespace HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
//
// public record EvolutionStrategyState : IAlgorithmState {
//   public required int Generation { get; init; }
//   public required IReadOnlyList<Solution<RealVector>> Population { get; init; }
//   public required double MutationStrength { get; init; }
// }
//
// public record EvolutionStrategyOperatorMetrics {
//   public OperatorMetric Creation { get; init; } = OperatorMetric.Zero;
//   // public OperatorMetric Decoding { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Evaluation { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Selection { get; init; } = OperatorMetric.Zero;
//   //public OperatorMetric Crossover { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Mutation { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Replacement { get; init; } = OperatorMetric.Zero;
//   public OperatorMetric Interception { get; init; } = OperatorMetric.Zero;
//   
//   public static EvolutionStrategyOperatorMetrics Aggregate(EvolutionStrategyOperatorMetrics left, EvolutionStrategyOperatorMetrics right) {
//     return new EvolutionStrategyOperatorMetrics {
//       Creation = left.Creation + right.Creation,
//       // Decoding = left.Decoding + right.Decoding,
//       Evaluation = left.Evaluation + right.Evaluation,
//       Selection = left.Selection + right.Selection,
//       //Crossover = a.Crossover + b.Crossover,
//       Mutation = left.Mutation + right.Mutation,
//       Replacement = left.Replacement + right.Replacement,
//       Interception = left.Interception + right.Interception
//     };
//   }
//   public static EvolutionStrategyOperatorMetrics operator +(EvolutionStrategyOperatorMetrics left, EvolutionStrategyOperatorMetrics right) => Aggregate(left, right);
// }
//
// public record EvolutionStrategyIterationResult {
//   public required int Generation { get; init; }
//   public required TimeSpan Duration { get; init; }
//   public required EvolutionStrategyOperatorMetrics OperatorMetrics { get; init; }
//   public required double MutationStrength { get; init; }
//   public required Objective Objective { get; init; }
//   public required IReadOnlyList<Solution<RealVector>> Population { get; init; }
// }
//
// public record EvolutionStrategyResult : ISingleObjectiveAlgorithmResult<RealVector>, IContinuableAlgorithmResult<EvolutionStrategyState> {
//   int IAlgorithmResult.CurrentIteration => CurrentGeneration;
//   int IAlgorithmResult.TotalIterations => TotalGenerations;
//   
//   public required int CurrentGeneration { get; init; }
//   public required int TotalGenerations { get; init; }
//   
//   public required TimeSpan CurrentDuration { get; init; }
//   public required TimeSpan TotalDuration { get; init; }
//   
//   public required double CurrentMutationStrength { get; init; }
//   
//   public required EvolutionStrategyOperatorMetrics CurrentOperatorMetrics { get; init; }
//   public required EvolutionStrategyOperatorMetrics TotalOperatorMetrics { get; init; }
//
//   public required Objective Objective { get; init; }
//   
//   public required IReadOnlyList<Solution<RealVector>> CurrentPopulation { get; init; }
//   
//   // public EvolutionStrategyResult() {
//   //   currentBestSolution = new Lazy<EvaluatedIndividual<RealVector>>(() => {
//   //     if (CurrentPopulation!.Count == 0) throw new InvalidOperationException("Population is empty");
//   //     return CurrentPopulation.MinBy(x => x.Fitness, Objective!.TotalOrderComparer)!;
//   //   });
//   // }
//   //
//   // private readonly Lazy<EvaluatedIndividual<RealVector>> currentBestSolution;
//   // public EvaluatedIndividual<RealVector> CurrentBestSolution => currentBestSolution.Value;
//   
//   public required Solution<RealVector>? CurrentBestSolution { get; init; }
//   public required Solution<RealVector>? BestSolution { get; init; }
//   
//   public EvolutionStrategyState GetContinuationState() => new() {
//     Generation = CurrentGeneration,
//     MutationStrength = CurrentMutationStrength,
//     Population = CurrentPopulation
//   };
//
//   public EvolutionStrategyState GetRestartState() => GetContinuationState() with { Generation = 0 };
// }
//
// public record class EvolutionStrategy<TProblem>
//   : IterativeAlgorithm<RealVector, TProblem, EvolutionStrategyState, EvolutionStrategyIterationResult, EvolutionStrategyResult>
//   where TProblem : IOptimizable<RealVector>
// {
//   public int PopulationSize { get; init;  }
//   public int Children { get; init;  }
//   public EvolutionStrategyType Strategy { get; init; }
//
//   public Creator<RealVector, TProblem> Creator { get; init; }
//
//   //public ICrossover<RealVector, RealVectorSearchSpace>? Crossover { get; }
//   public Mutator<RealVector, TProblem> Mutator { get; }
//   public double InitialMutationStrength { get; init;  }
//   public Selector<TProblem> Selector { get; init;  }
//   public int RandomSeed { get; init;  }
//   public Interceptor<TProblem, EvolutionStrategyIterationResult>? Interceptor { get; init; }
//
//   public EvolutionStrategy(
//     int populationSize,
//     int children,
//     EvolutionStrategyType strategy,
//     Creator<RealVector, TProblem> creator,
//     //ICrossover<RealVector, RealVectorSearchSpace> crossover,
//     Mutator<RealVector, TProblem> mutator,
//     double initialMutationStrength,
//     Selector<TProblem> selector,
//     int randomSeed,
//     Terminator<TProblem, EvolutionStrategyResult> terminator,
//     Interceptor<TProblem, EvolutionStrategyIterationResult>? interceptor = null)
//     : base(terminator) {
//     PopulationSize = populationSize;
//     Children = children;
//     Strategy = strategy;
//     Creator = creator;
//     //Crossover = crossover;
//     Mutator = mutator;
//     InitialMutationStrength = initialMutationStrength;
//     Selector = selector;
//     RandomSeed = randomSeed;
//     Interceptor = interceptor;
//   }
//
//   public override EvolutionStrategyExecution<TProblem> CreateStreamingExecution(TProblem problem) {
//     return new EvolutionStrategyExecution<TProblem>(this, problem);
//   }
// }
//
// public class EvolutionStrategyExecution<TProblem>
//   : IterativeAlgorithmExecution<RealVector, TProblem, EvolutionStrategyState, EvolutionStrategyIterationResult, EvolutionStrategyResult, EvolutionStrategy<TProblem>>
//   where TProblem : IOptimizable<RealVector>
// {
//   public IRandomNumberGenerator Random { get; }
//   public ICreatorExecution<RealVector> Creator { get; }
//   //public ICrossoverInstance<RealVector, RealVectorSearchSpace>? Crossover { get; }
//   public IMutatorExecution<RealVector> Mutator { get; }
//   public ISelectorExecution Selector { get; }
//   public IInterceptorExecution<EvolutionStrategyIterationResult>? Interceptor { get; }
//   
//   public EvolutionStrategyExecution(EvolutionStrategy<TProblem> parameters, TProblem problem) : base(parameters, problem) {
//     Random = new SystemRandomNumberGenerator(parameters.RandomSeed);
//     Creator = parameters.Creator.CreateExecution(problem);
//     //Crossover = parameters.Crossover?.CreateInstance();
//     Mutator = parameters.Mutator.CreateExecution(problem);
//     Selector = parameters.Selector.CreateExecution(problem);
//     Interceptor = parameters.Interceptor?.CreateExecution(problem);
//   }
//   
//   // public override EvolutionStrategyResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, RealVector, RealVectorSearchSpace> problem, EvolutionStrategyState? initialState = null) {
//   //   EvaluatedIndividual<RealVector>? bestSolution = null;
//   //   var comparer = problem.Objective.TotalOrderComparer;
//   //   
//   //   int totalGenerations = 0;
//   //   TimeSpan totalDuration = TimeSpan.Zero;
//   //   var totalMetrics = new EvolutionStrategyOperatorMetrics();
//   //   
//   //   foreach (var result in ExecuteStreaming(problem, initialState)) {
//   //     if (bestSolution is null || comparer.Compare(bestSolution.Fitness, result.BestSolution.Fitness) < 0) 
//   //       bestSolution = result.BestSolution;
//   //     totalGenerations += 1;
//   //     totalDuration += result.TotalDuration;
//   //     totalMetrics += result.OperatorMetrics;
//   //   }
//   //   
//   //   return new EvolutionStrategyResult() {
//   //     TotalGenerations = totalGenerations,
//   //     TotalDuration = totalDuration,
//   //     OperatorMetrics = totalMetrics,
//   //     BestSolution = bestSolution
//   //   };
//   // }
//   
//   protected override EvolutionStrategyIterationResult ExecuteInitialization() {
//     var start = Stopwatch.GetTimestamp();
//
//     //var random = new SystemRandomNumberGenerator(Algorithm.RandomSeed);
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
//     var result = new EvolutionStrategyIterationResult() {
//       Generation = 0,
//       Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
//       MutationStrength = Parameters.InitialMutationStrength,
//       Objective = Problem.Objective,
//       Population = population,
//       OperatorMetrics = new () {
//         Creation = new(genotypes.Length, Stopwatch.GetElapsedTime(startCreating, endCreating)),
//         // Decoding = new(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
//         Evaluation = new(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating)),
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
//     
//     return interceptedResult with {
//       Duration = Stopwatch.GetElapsedTime(start, end),
//       OperatorMetrics = interceptedResult.OperatorMetrics with {
//         Interception = new(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
//       } 
//     };
//   }
//   
//   protected override EvolutionStrategyIterationResult ExecuteIteration(EvolutionStrategyState state) {
//     var start = Stopwatch.GetTimestamp();
//
//     // int newRandomSeed = SeedSequence.GetSeed(Algorithm.RandomSeed, state.Generation);
//     // var random = new SystemRandomNumberGenerator(newRandomSeed);
//     
//     var oldPopulation = state.Population;
//     
//     var startSelection = Stopwatch.GetTimestamp();
//     var randomSelector = new RandomSelector().CreateExecution(); // improve
//     var parents = randomSelector.Select(oldPopulation, Problem.Objective, Parameters.PopulationSize, Random).ToList();
//     var endSelection = Stopwatch.GetTimestamp();
//      
//     var genotypes = new RealVector[parents.Count];
//     var startMutation = Stopwatch.GetTimestamp();
//     for (int i = 0; i < parents.Count; i += 2) {
//       var parent = parents[i];
//       var child = Mutator.Mutate(parent.Genotype, Random);
//       genotypes[i / 2] = child;
//     }
//     var endMutation = Stopwatch.GetTimestamp();
//     
//     // ToDo: optional crossover
//     
//     // var startDecoding = Stopwatch.GetTimestamp();
//     // var phenotypePopulation = genotypePopulation.Select(genotype => optimizable.Decoder.Decode(genotype)).ToArray();
//     // var endDecoding = Stopwatch.GetTimestamp();
//     
//     var startEvaluation = Stopwatch.GetTimestamp();
//     var fitnesses = genotypes.Select(genotype => Problem.Evaluate(genotype)).ToArray();
//     var endEvaluation = Stopwatch.GetTimestamp();
//     
//     // timing the adaption check
//     int successfulOffspring = 0;
//     for (int i = 0; i < fitnesses.Length; i++) {
//       if (fitnesses[i].CompareTo(parents[i].ObjectiveVector, Problem.Objective) == DominanceRelation.Dominates) {
//         successfulOffspring++;
//       }
//     }
//     double successRate = (double)successfulOffspring / genotypes.Length;
//     double newMutationStrength = successRate switch {
//       > 0.2 => state.MutationStrength * 1.5,
//       < 0.2 => state.MutationStrength / 1.5,
//       _ => state.MutationStrength
//     };
//     
//     var population = Population.From(genotypes, /*phenotypePopulation,*/ fitnesses);
//     
//     var startReplacement = Stopwatch.GetTimestamp();
//     // ToDo: to create execution/instance
//     Replacer<TProblem> replacer = Parameters.Strategy switch {
//       EvolutionStrategyType.Comma => new ElitismReplacer(0),
//       EvolutionStrategyType.Plus => new PlusSelectionReplacer(),
//       _ => throw new InvalidOperationException($"Unknown strategy {Parameters.Strategy}")
//     };
//     var newPopulation = replacer.CreateExecution(Problem).Replace(oldPopulation, population, Problem.Objective, Random);
//     var endReplacement = Stopwatch.GetTimestamp();
//     
//     var endBeforeInterceptor = Stopwatch.GetTimestamp();
//
//     var result = new EvolutionStrategyIterationResult() {
//       Generation = state.Generation + 1,
//       Duration = Stopwatch.GetElapsedTime(start, endBeforeInterceptor),
//       MutationStrength = newMutationStrength,
//       Objective = Problem.Objective,
//       Population = newPopulation,
//       OperatorMetrics = new() {
//         Selection = new(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection)),
//         Mutation = new(genotypes.Length, Stopwatch.GetElapsedTime(startMutation, endMutation)),
//         // Decoding = new(phenotypePopulation.Length, Stopwatch.GetElapsedTime(startDecoding, endDecoding)),
//         Evaluation = new (fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation)),
//         Replacement = new (1, Stopwatch.GetElapsedTime(startReplacement, endReplacement)),
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
//         Interception = new(1, Stopwatch.GetElapsedTime(interceptorStart, interceptorEnd))
//       }
//     };
//   }
//   protected override EvolutionStrategyResult AggregateResult(EvolutionStrategyIterationResult iterationResult, EvolutionStrategyResult? algorithmResult) {
//     var currentBestSolution = iterationResult.Population.MinBy(x => x.ObjectiveVector, iterationResult.Objective.TotalOrderComparer);
//     return new EvolutionStrategyResult() {
//       CurrentGeneration = iterationResult.Generation,
//       TotalGenerations = iterationResult.Generation,
//       CurrentDuration = iterationResult.Duration,
//       TotalDuration = iterationResult.Duration + (algorithmResult?.TotalDuration ?? TimeSpan.Zero),
//       CurrentMutationStrength = iterationResult.MutationStrength,
//       CurrentOperatorMetrics = iterationResult.OperatorMetrics,
//       TotalOperatorMetrics = iterationResult.OperatorMetrics + (algorithmResult?.TotalOperatorMetrics ?? new EvolutionStrategyOperatorMetrics()),
//       Objective = iterationResult.Objective,
//       CurrentPopulation = iterationResult.Population,
//       CurrentBestSolution = currentBestSolution,
//       BestSolution = algorithmResult is null ? currentBestSolution : new[] {algorithmResult.BestSolution, currentBestSolution}.MinBy(x => x.ObjectiveVector, iterationResult.Objective.TotalOrderComparer)
//     };
//   }
// }
