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
  public Task EvolutionStrategyAndGeneticAlgorithm_SolveRealVectorTestFunctionProblem() {
    var problem = new RealVectorTestFunctionProblem(RealVectorTestFunctionProblem.FunctionType.Sphere, -5.0, 5.0);
    var encoding = problem.CreateRealVectorEncodingEncoding();
    var evaluator = problem.CreateEvaluator();
    var randomSource = new SeededRandomSource(42);

    var evolutionStrategy = new EvolutionStrategy(
      populationSize: 10,
      children: 20,
      strategy: EvolutionStrategyType.Comma,
      creator: new NormalDistributedCreator(encoding, 1.0, 0.0, randomSource),
      mutator: new GaussianMutator(encoding, mutationRate: 1.0, mutationStrength: 0.5, randomSource),
      initialMutationStrength: 0.1,
      crossover: null,
      evaluator: evaluator,
      terminator: new ThresholdTerminator<EvolutionStrategyPopulationState>(10, state => state.Generation),
      randomSource: randomSource
    );

    var geneticAlgorithm = new GeneticAlgorithm<RealVector>(
      populationSize: 10,
      creator: new UniformDistributedCreator(encoding, null, null, randomSource),
      crossover: new AlphaBetaBlendCrossover(encoding, 0.8, 0.2),
      mutator: new GaussianMutator(encoding, mutationRate: 10, mutationStrength: 1.5, randomSource),
      mutationRate: 0.1,
      terminator: new ThresholdTerminator<PopulationState<RealVector>>(10, state => state.Generation),
      evaluator: evaluator,
      randomSourceState: randomSource,
      selector: new TournamentSelector<RealVector>(2, randomSource),
      replacer: new ElitismReplacer<RealVector>(1)
    );

    var cyclicAlgorithm = new CyclicAlgorithm<IState, EvolutionStrategyPopulationState, PopulationState<RealVector>>(
      firstAlgorithm: evolutionStrategy,
      secondAlgorithm: geneticAlgorithm,
      transformer: new EvolutionToGeneticStateTransformer(),
      repetitionTransformer: new GeneticToEvolutionStateTransformer()
    );

    var finalState = cyclicAlgorithm.Execute(termination: new Terminator());

    return Verify(finalState);
  }
  
  class EvolutionToGeneticStateTransformer : IStateTransformer<EvolutionStrategyPopulationState, PopulationState<RealVector>> {
    public PopulationState<RealVector> Transform(EvolutionStrategyPopulationState sourceState, PopulationState<RealVector>? previousTargetState = null) {
      return new PopulationState<RealVector> {
        Generation = sourceState.Generation,
        Population = sourceState.Population,
        Objectives = sourceState.Objectives
      };
    }
  }

  class GeneticToEvolutionStateTransformer : IStateTransformer<PopulationState<RealVector>, EvolutionStrategyPopulationState> {
    public EvolutionStrategyPopulationState Transform(PopulationState<RealVector> sourceState, EvolutionStrategyPopulationState? previousTargetState = null) {
      return new EvolutionStrategyPopulationState {
        Generation = sourceState.Generation,
        Population = sourceState.Population,
        Objectives = sourceState.Objectives,
        MutationStrength = previousTargetState?.MutationStrength ?? 0.1 // TODO: how to get mutation strength from the default of the ES?
      };
    }
  }
  
  class Terminator : ITerminator<IState> {
    public bool ShouldTerminate(IState state) {
      return state switch {
        EvolutionStrategyPopulationState esState => esState.Generation >= 10,
        PopulationState<RealVector> gaState => gaState.Generation >= 10,
        _ => throw new ArgumentException("Unknown state type.")
      };
    }
  }
}
