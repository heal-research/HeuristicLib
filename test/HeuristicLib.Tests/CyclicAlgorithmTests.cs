using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class CyclicAlgorithmTests {
  [Fact]
  public Task ConcatAlgorithm_WithGA() {
    var problem = new RealVectorTestFunctionProblem(RealVectorTestFunctionProblem.FunctionType.Sphere, -5.0, 5.0);
    var encoding = problem.CreateRealVectorEncodingEncoding();
    var evaluator = problem.CreateEvaluator();
    var randomSource = new RandomSource(42);

    var ga1 = new GeneticAlgorithm<RealVector>(
      populationSize: 2,
      creator: new UniformDistributedCreator(encoding, null, null, randomSource),
      crossover: new AlphaBetaBlendCrossover(encoding, 0.8, 0.2),
      mutator: new GaussianMutator(encoding, mutationRate: 10, mutationStrength: 1.5, randomSource),
      mutationRate: 0.1,
      evaluator: evaluator,
      Goal.Minimize,
      selector: new TournamentSelector<RealVector>(2, randomSource),
      replacer: new ElitismReplacer<RealVector>(1), 
      randomSourceState: randomSource,
      terminator: Terminator.OnGeneration(3)
    );
    
    var ga2 = new GeneticAlgorithm<RealVector>(
      populationSize: 2,
      creator: new UniformDistributedCreator(encoding, null, null, randomSource),
      crossover: new AlphaBetaBlendCrossover(encoding, 0.5, 0.5),
      mutator: new GaussianMutator(encoding, mutationRate: 10, mutationStrength: 1.5, randomSource),
      mutationRate: 0.1,
      evaluator: evaluator,
      Goal.Minimize,
      selector: new RandomSelector<RealVector, Fitness, Goal>(randomSource),
      replacer: new ElitismReplacer<RealVector>(1), 
      randomSourceState: randomSource,
      terminator: Terminator.OnGeneration(3)
    );
    
    var ga3 = new GeneticAlgorithm<RealVector>(
      populationSize: 3,
      creator: new UniformDistributedCreator(encoding, null, null, randomSource),
      crossover: new AlphaBetaBlendCrossover(encoding, 0.8, 0.2),
      mutator: new GaussianMutator(encoding, mutationRate: 10, mutationStrength: 1.5, randomSource),
      mutationRate: 0.8,
      evaluator: evaluator,
      Goal.Minimize,
      selector: new TournamentSelector<RealVector>(2, randomSource),
      replacer: new ElitismReplacer<RealVector>(1), 
      randomSourceState: randomSource,
      terminator: Terminator.OnGeneration(4)
    );

    var concatAlgorithm = new ConcatAlgorithm<PopulationState<RealVector, Fitness, Goal>>(ga1, ga2, ga3);

    var states = concatAlgorithm.CreateExecutionStream().ToList();
    var lastState = states[^1];
    var generations = states.Select(x => x.Generation).ToList();
    
    states.Count.ShouldBe(1 + 10);
    
    return Verify(new { generations, lastState });
  }
  
  [Fact]
  public Task CyclicAlgorithm_WithGA() {
    var problem = new RealVectorTestFunctionProblem(RealVectorTestFunctionProblem.FunctionType.Sphere, -5.0, 5.0);
    var encoding = problem.CreateRealVectorEncodingEncoding();
    var evaluator = problem.CreateEvaluator();
    var randomSource = new RandomSource(42);

    var ga1 = new GeneticAlgorithm<RealVector>(
      populationSize: 2,
      creator: new UniformDistributedCreator(encoding, null, null, randomSource),
      crossover: new AlphaBetaBlendCrossover(encoding, 0.8, 0.2),
      mutator: new GaussianMutator(encoding, mutationRate: 10, mutationStrength: 1.5, randomSource),
      mutationRate: 0.1,
      evaluator: evaluator,
      Goal.Minimize,
      selector: new TournamentSelector<RealVector>(2, randomSource),
      replacer: new ElitismReplacer<RealVector>(1), 
      randomSourceState: randomSource,
      terminator: Terminator.OnGeneration(3)
    );

    var ga2 = new GeneticAlgorithm<RealVector>(
      populationSize: 2,
      creator: new UniformDistributedCreator(encoding, null, null, randomSource),
      crossover: new AlphaBetaBlendCrossover(encoding, 0.5, 0.5),
      mutator: new GaussianMutator(encoding, mutationRate: 10, mutationStrength: 1.5, randomSource),
      mutationRate: 0.1,
      evaluator: evaluator,
      Goal.Minimize,
      selector: new RandomSelector<RealVector, Fitness, Goal>(randomSource),
      replacer: new ElitismReplacer<RealVector>(1), 
      randomSourceState: randomSource,
      terminator: Terminator.OnGeneration(3)
    );
    
    var ga3 = new GeneticAlgorithm<RealVector>(
      populationSize: 3,
      creator: new UniformDistributedCreator(encoding, null, null, randomSource),
      crossover: new AlphaBetaBlendCrossover(encoding, 0.8, 0.2),
      mutator: new GaussianMutator(encoding, mutationRate: 10, mutationStrength: 1.5, randomSource),
      mutationRate: 0.8,
      evaluator: evaluator,
      Goal.Minimize,
      selector: new TournamentSelector<RealVector>(2, randomSource),
      replacer: new ElitismReplacer<RealVector>(1), 
      randomSourceState: randomSource,
      terminator: Terminator.OnGeneration(4)
    );

    var concatAlgorithm = new CyclicAlgorithm<PopulationState<RealVector, Fitness, Goal>>(ga1, ga2, ga3);

    var states = concatAlgorithm.CreateExecutionStream().Take(25).ToList();
    var lastState = states[^1];
    var generations = states.Select(s => s.Generation).ToList();
    
    return Verify(new { generations, lastState });
  }
  
  
  [Fact]
  public Task EvolutionStrategyAndGeneticAlgorithm_SolveRealVectorTestFunctionProblem() {
    var problem = new RealVectorTestFunctionProblem(RealVectorTestFunctionProblem.FunctionType.Sphere, -5.0, 5.0);
    var encoding = problem.CreateRealVectorEncodingEncoding();
    var evaluator = problem.CreateEvaluator();
    var randomSource = new RandomSource(42);

    var evolutionStrategy = new EvolutionStrategy(
      populationSize: 10,
      children: 20,
      strategy: EvolutionStrategyType.Comma,
      creator: new NormalDistributedCreator(encoding, 0.0, 1.0, randomSource),
      mutator: new GaussianMutator(encoding, mutationRate: 1.0, mutationStrength: 0.5, randomSource),
      initialMutationStrength: 0.1,
      crossover: null,
      evaluator: evaluator,
      Goal.Minimize,
      terminator: Terminator.OnGeneration(6),
      randomSource: randomSource
    );

    var geneticAlgorithm = new GeneticAlgorithm<RealVector>(
      populationSize: 10,
      creator: new UniformDistributedCreator(encoding, null, null, randomSource),
      crossover: new AlphaBetaBlendCrossover(encoding, 0.8, 0.2),
      mutator: new GaussianMutator(encoding, mutationRate: 10, mutationStrength: 1.5, randomSource),
      mutationRate: 0.1,
      evaluator: evaluator,
      Goal.Minimize,
      selector: new TournamentSelector<RealVector>(2, randomSource),
      replacer: new ElitismReplacer<RealVector>(1), randomSourceState: randomSource, 
      terminator: Terminator.OnGeneration(4));

    var cyclicAlgorithm = new CyclicAlgorithm<IState, EvolutionStrategyPopulationState, PopulationState<RealVector, Fitness, Goal>>(
      firstAlgorithm: evolutionStrategy,
      secondAlgorithm: geneticAlgorithm,
      transformer: new EvolutionToGeneticStateTransformer(),
      repetitionTransformer: StateTransformer.Create((PopulationState<RealVector, Fitness, Goal> sourceState, EvolutionStrategyPopulationState? previousTargetState) => {
        previousTargetState ??= new EvolutionStrategyPopulationState() { Goal = Goal.Minimize, Population = sourceState.Population, MutationStrength = 0.1 };
        return previousTargetState.Reset() with {
          Population = sourceState.Population, MutationStrength = previousTargetState.MutationStrength
        };
      })
    );

    var states = cyclicAlgorithm.CreateExecutionStream().Take(25).ToList();
    var lastState = states[^1];
    var generations = states.Select(s => new { Generation = s switch {
        EvolutionStrategyPopulationState esState => esState.Generation,
        PopulationState<RealVector, Fitness, Goal> gaState =>  gaState.Generation,
        _ => throw new NotImplementedException()
        }, Type = s.GetType() })
      .ToList();
    
    return Verify(new { generations, lastState });
  }

  private class EvolutionToGeneticStateTransformer : IStateTransformer<EvolutionStrategyPopulationState, PopulationState<RealVector, Fitness, Goal>> {
    public PopulationState<RealVector, Fitness, Goal> Transform(EvolutionStrategyPopulationState sourceState, PopulationState<RealVector, Fitness, Goal>? previousTargetState = null) {
      previousTargetState ??= new PopulationState<RealVector, Fitness, Goal>() { Goal = Goal.Minimize, Population = sourceState.Population };
      return previousTargetState.Reset() with {
        Population = sourceState.Population
      };
    }
  }
}
