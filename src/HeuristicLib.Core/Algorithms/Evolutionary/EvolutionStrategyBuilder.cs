using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record EvolutionStrategyBuilder<TG, TS, TP>
  : AlgorithmBuilder<TG, TS, TP, PopulationState<TG>, EvolutionStrategy<TG, TS, TP>>,
    IBuilderWithCreator<TG, TS, TP>,
    IBuilderWithMutator<TG, TS, TP>,
    IBuilderWithSelector<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
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
    InitialMutationStrength = InitialMutationStrength,
    Interceptor = Interceptor,
    NumberOfChildren = NumberOfChildren
  };
}
