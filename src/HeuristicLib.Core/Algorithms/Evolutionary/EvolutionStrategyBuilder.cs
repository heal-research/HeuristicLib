using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record EvolutionStrategyBuilder<TG, TS, TP> : AlgorithmBuilder<TG, TS, TP, EvolutionStrategyState<TG>, EvolutionStrategy<TG, TS, TP>, EvolutionStrategyBuildSpec<TG, TS, TP>>
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

  public override EvolutionStrategyBuildSpec<TG, TS, TP> CreateBuildSpec() => new(
    Evaluator, Terminator, Interceptor, PopulationSize, Strategy, Mutator, InitialMutationStrength, Crossover, Selector, NumberOfChildren, Creator
  );

  public override EvolutionStrategy<TG, TS, TP> BuildFromSpec(EvolutionStrategyBuildSpec<TG, TS, TP> spec) => new() {
    PopulationSize = spec.PopulationSize,
    Strategy = spec.Strategy,
    Creator = spec.Creator,
    Mutator = spec.Mutator,
    Crossover = spec.Crossover,
    Selector = spec.Selector,
    Evaluator = spec.Evaluator,
    //Terminator = spec.Terminator,
    InitialMutationStrength = spec.InitialMutationStrength,
    Interceptor = spec.Interceptor,
    NumberOfChildren = spec.NumberOfChildren
  };
}
