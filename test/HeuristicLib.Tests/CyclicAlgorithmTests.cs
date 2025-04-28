using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests;

public class CyclicAlgorithmTests {
  // [Fact]
  // public Task ConcatAlgorithm_ExecuteWithGAs() {
  //   var problem = new TestFunctionProblem(new SphereFunction(2));
  //
  //   var ga1 = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
  //     populationSize: 2,
  //     creator: new UniformDistributedCreator(),
  //     crossover: new AlphaBetaBlendCrossover(0.8, 0.2),
  //     mutator: new GaussianMutator(mutationRate: 10, mutationStrength: 1.5),
  //     mutationRate: 0.1,
  //     selector: new TournamentSelector(2),
  //     replacer: new ElitismReplacer(1), 
  //     randomSeed: 42,
  //     terminator: Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(3)
  //   );
  //   
  //   var ga2 = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
  //     populationSize: 2,
  //     creator: new UniformDistributedCreator(),
  //     crossover: new AlphaBetaBlendCrossover(0.5, 0.5),
  //     mutator: new GaussianMutator(mutationRate: 10, mutationStrength: 1.5),
  //     mutationRate: 0.1,
  //     selector: new RandomSelector(),
  //     replacer: new ElitismReplacer(1), 
  //     randomSeed: 42,
  //     terminator: Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(3)
  //   );
  //   
  //   var ga3 = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
  //     populationSize: 3,
  //     creator: new UniformDistributedCreator(),
  //     crossover: new AlphaBetaBlendCrossover(0.8, 0.2),
  //     mutator: new GaussianMutator(mutationRate: 10, mutationStrength: 1.5),
  //     mutationRate: 0.8,
  //     selector: new TournamentSelector(2),
  //     replacer: new ElitismReplacer(1), 
  //     randomSeed: 42,
  //     terminator: Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(4)
  //   );
  //
  //   var concatAlgorithm = new ConcatAlgorithm<RealVector, RealVectorEncoding, GeneticAlgorithmState<RealVector>, GeneticAlgorithmIterationResult<RealVector>>([ga1, ga2, ga3]);
  //
  //   var result = concatAlgorithm.Execute(problem);
  //   
  //   return Verify(result)
  //     .IgnoreMembersWithType<TimeSpan>();
  // }
  
  [Fact]
  public Task ConcatAlgorithm_ExecuteStreamingWithGAs() {
    var problem = new TestFunctionProblem(new SphereFunction(2));

    var ga1 = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      populationSize: 2,
      creator: new UniformDistributedCreator(),
      crossover: new AlphaBetaBlendCrossover(0.8, 0.2),
      mutator: new GaussianMutator(mutationRate: 10, mutationStrength: 1.5),
      mutationRate: 0.1,
      selector: new TournamentSelector(2),
      replacer: new ElitismReplacer(1), 
      randomSeed: 42,
      terminator: Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(3)
    );
    
    var ga2 = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      populationSize: 2,
      creator: new UniformDistributedCreator(),
      crossover: new AlphaBetaBlendCrossover(0.5, 0.5),
      mutator: new GaussianMutator(mutationRate: 10, mutationStrength: 1.5),
      mutationRate: 0.1,
      selector: new RandomSelector(),
      replacer: new ElitismReplacer(1), 
      randomSeed: 42,
      terminator: Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(3)
    );
    
    var ga3 = new GeneticAlgorithm<RealVector, RealVectorEncoding>(
      populationSize: 3,
      creator: new UniformDistributedCreator(),
      crossover: new AlphaBetaBlendCrossover(0.8, 0.2),
      mutator: new GaussianMutator(mutationRate: 10, mutationStrength: 1.5),
      mutationRate: 0.8,
      selector: new TournamentSelector(2),
      replacer: new ElitismReplacer(1), 
      randomSeed: 42,
      terminator: Terminator.OnGeneration<GeneticAlgorithmIterationResult<RealVector>>(4)
    );

    var concatAlgorithm = new ConcatAlgorithm<RealVector, RealVectorEncoding, GeneticAlgorithmState<RealVector>, GeneticAlgorithmIterationResult<RealVector>, GeneticAlgorithmResult<RealVector>>([ga1, ga2, ga3]);

    var states = concatAlgorithm.ExecuteStreaming(problem).ToList();
    var lastState = states[^1];
    var generations = states.Select(x => x.Generation).ToList();
    
    states.Count.ShouldBe(1+3 + 3 + 4);
    
    return Verify(new { generations, lastState })
      .IgnoreMembersWithType<TimeSpan>();
  }
  
  // [Fact]
  // public Task CyclicAlgorithm_WithGA() {
  //   var problem = new RealVectorTestFunctionProblem(RealVectorTestFunctionProblem.FunctionType.Sphere, -5.0, 5.0);
  //   var encoding = problem.CreateRealVectorEncoding();
  //   var evaluator = Evaluator.UsingFitnessFunction<RealVector>(problem.Evaluate);
  //   var randomSource = new RandomSource(42);
  //
  //   var ga1 = new GeneticAlgorithm<RealVector>(
  //     populationSize: 2,
  //     creator: new UniformDistributedCreator(null, null, encoding, randomSource.CreateRandomNumberGenerator()),
  //     crossover: new AlphaBetaBlendCrossoverOperator(0.8, 0.2),
  //     mutator: new GaussianMutator(mutationRate: 10, mutationStrength: 1.5, encoding, randomSource.CreateRandomNumberGenerator()),
  //     mutationRate: 0.1,
  //     evaluator: evaluator,
  //     SingleObjective.Minimize,
  //     selector: new TournamentSelector(2, randomSource.CreateRandomNumberGenerator()),
  //     replacer: new ElitismReplacer(1), 
  //     randomSourceState: randomSource,
  //     terminator: Terminator.OnGeneration(3)
  //   );
  //
  //   var ga2 = new GeneticAlgorithm<RealVector>(
  //     populationSize: 2,
  //     creator: new UniformDistributedCreator(null, null, encoding, randomSource.CreateRandomNumberGenerator()),
  //     crossover: new AlphaBetaBlendCrossoverOperator(0.5, 0.5),
  //     mutator: new GaussianMutator(mutationRate: 10, mutationStrength: 1.5, encoding, randomSource.CreateRandomNumberGenerator()),
  //     mutationRate: 0.1,
  //     evaluator: evaluator,
  //     SingleObjective.Minimize,
  //     selector: new RandomSelector(randomSource.CreateRandomNumberGenerator()),
  //     replacer: new ElitismReplacer(1), 
  //     randomSourceState: randomSource,
  //     terminator: Terminator.OnGeneration(3)
  //   );
  //   
  //   var ga3 = new GeneticAlgorithm<RealVector>(
  //     populationSize: 3,
  //     creator: new UniformDistributedCreator(null, null, encoding, randomSource.CreateRandomNumberGenerator()),
  //     crossover: new AlphaBetaBlendCrossoverOperator(0.8, 0.2),
  //     mutator: new GaussianMutator(mutationRate: 10, mutationStrength: 1.5, encoding, randomSource.CreateRandomNumberGenerator()),
  //     mutationRate: 0.8,
  //     evaluator: evaluator,
  //     SingleObjective.Minimize,
  //     selector: new TournamentSelector(2, randomSource.CreateRandomNumberGenerator()),
  //     replacer: new ElitismReplacer(1), 
  //     randomSourceState: randomSource,
  //     terminator: Terminator.OnGeneration(4)
  //   );
  //
  //   var concatAlgorithm = new CyclicAlgorithm<PopulationState<RealVector>>(ga1, ga2, ga3);
  //
  //   var states = concatAlgorithm.CreateExecutionStream().Take(25).ToList();
  //   var lastState = states[^1];
  //   var generations = states.Select(s => s.Generation).ToList();
  //   
  //   return Verify(new { generations, lastState });
  // }
  //
  //
  // [Fact]
  // public Task EvolutionStrategyAndGeneticAlgorithm_SolveRealVectorTestFunctionProblem() {
  //   var problem = new RealVectorTestFunctionProblem(RealVectorTestFunctionProblem.FunctionType.Sphere, -5.0, 5.0);
  //   var encoding = problem.CreateRealVectorEncoding();
  //   var evaluator = Evaluator.UsingFitnessFunction<RealVector>(problem.Evaluate);
  //   var randomSource = new RandomSource(42);
  //
  //   var evolutionStrategy = new EvolutionStrategy(
  //     populationSize: 10,
  //     children: 20,
  //     strategy: EvolutionStrategyType.Comma,
  //     creator: new NormalDistributedCreator(0.0, 1.0, encoding, randomSource.CreateRandomNumberGenerator()),
  //     mutator: new GaussianMutator(mutationRate: 1.0, mutationStrength: 0.5, encoding, randomSource.CreateRandomNumberGenerator()),
  //     initialMutationStrength: 0.1,
  //     crossover: null,
  //     evaluator: evaluator,
  //     SingleObjective.Minimize,
  //     terminator: Terminator.OnGeneration(6),
  //     randomSource: randomSource
  //   );
  //
  //   var geneticAlgorithm = new GeneticAlgorithm<RealVector>(
  //     populationSize: 10,
  //     creator: new UniformDistributedCreator(null, null, encoding, randomSource.CreateRandomNumberGenerator()),
  //     crossover: new AlphaBetaBlendCrossoverOperator(0.8, 0.2),
  //     mutator: new GaussianMutator(mutationRate: 10, mutationStrength: 1.5, encoding, randomSource.CreateRandomNumberGenerator()),
  //     mutationRate: 0.1,
  //     evaluator: evaluator,
  //     SingleObjective.Minimize,
  //     selector: new TournamentSelector(2, randomSource.CreateRandomNumberGenerator()),
  //     replacer: new ElitismReplacer(1), 
  //     randomSourceState: randomSource, 
  //     terminator: Terminator.OnGeneration(4));
  //
  //   var cyclicAlgorithm = new CyclicAlgorithm<IState, EvolutionStrategyPopulationState, PopulationState<RealVector>>(
  //     firstAlgorithm: evolutionStrategy,
  //     secondAlgorithm: geneticAlgorithm,
  //     transformer: new EvolutionToGeneticStateTransformer(),
  //     repetitionTransformer: StateTransformer.Create((PopulationState<RealVector> sourceState, EvolutionStrategyPopulationState? previousTargetState) => {
  //       previousTargetState ??= new EvolutionStrategyPopulationState() { Objective = SingleObjective.Minimize, Population = sourceState.Population, MutationStrength = 0.1 };
  //       return previousTargetState.Reset() with {
  //         Population = sourceState.Population, MutationStrength = previousTargetState.MutationStrength
  //       };
  //     })
  //   );
  //
  //   var states = cyclicAlgorithm.CreateExecutionStream().Take(25).ToList();
  //   var lastState = states[^1];
  //   var generations = states.Select(s => new { Generation = s switch {
  //       EvolutionStrategyPopulationState esState => esState.Generation,
  //       PopulationState<RealVector> gaState =>  gaState.Generation,
  //       _ => throw new NotImplementedException()
  //       }, Type = s.GetType() })
  //     .ToList();
  //   
  //   return Verify(new { generations, lastState });
  // }
  //
  // private class EvolutionToGeneticStateTransformer : IStateTransformer<EvolutionStrategyPopulationState, PopulationState<RealVector>> {
  //   public PopulationState<RealVector> Transform(EvolutionStrategyPopulationState sourceState, PopulationState<RealVector>? previousTargetState = null) {
  //     previousTargetState ??= new PopulationState<RealVector>() { Objective = SingleObjective.Minimize, Population = sourceState.Population };
  //     return previousTargetState.Reset() with {
  //       Population = sourceState.Population
  //     };
  //   }
  // }
}
