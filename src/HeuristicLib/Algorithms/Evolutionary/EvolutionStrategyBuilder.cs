using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record EvolutionStrategyBuilder<TG, TS, TP> : AlgorithmBuilder<TG, TS, TP, EvolutionStrategyIterationState<TG>, EvolutionStrategy<TG, TS, TP>> 
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TG : class 
{
  public int PopulationSize { get; set; } = 100;
  public EvolutionStrategyType Strategy { get; set; } = EvolutionStrategyType.Plus;
  public required IMutator<TG, TS, TP> Mutator { get; set; }
  public required double InitialMutationStrength { get; set; }
  public ICrossover<TG, TS, TP>? Crossover { get; set; }
  public ISelector<TG, TS, TP> Selector { get; set; } = new RandomSelector<TG>();
  public int NumberOfChildren { get; set; } = 100;
  public required ICreator<TG, TS, TP> Creator { get; set; }
  
  public override EvolutionStrategy<TG, TS, TP> Build() => new() {
    PopulationSize = PopulationSize,
    Strategy = Strategy,
    Creator = Creator,
    Mutator = Mutator,
    Crossover = Crossover,
    Selector = Selector,
    Evaluator = Evaluator,
    Terminator = Terminator,
    AlgorithmRandom = SystemRandomNumberGenerator.Default(RandomSeed),
    InitialMutationStrength = InitialMutationStrength,
    Interceptor = Interceptor,
    NumberOfChildren = NumberOfChildren
  };
}
