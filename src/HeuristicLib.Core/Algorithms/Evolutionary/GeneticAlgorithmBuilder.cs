using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record GeneticAlgorithmBuilder<TG, TS, TP>
  : AlgorithmBuilder<TG, TS, TP, PopulationState<TG>, GeneticAlgorithm<TG, TS, TP>>,
    IBuilderWithCreator<TG, TS, TP>,
    IBuilderWithSelector<TG, TS, TP>,
    IBuilderWithCrossover<TG, TS, TP>,
    IBuilderWithMutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public int PopulationSize { get; set; } = 100;
  public ISelector<TG, TS, TP> Selector { get; set; } = new TournamentSelector<TG>(2);

  public required ICreator<TG, TS, TP> Creator { get; set; }
  public required ICrossover<TG, TS, TP> Crossover { get; set; }
  public required IMutator<TG, TS, TP> Mutator { get; set; }
  public double MutationRate { get; set; } = 0.05;

  public int Elites { get; set; } = 1;

  public override GeneticAlgorithm<TG, TS, TP> Build()
  {
    return new GeneticAlgorithm<TG, TS, TP> {
      PopulationSize = PopulationSize,
      Creator = Creator,
      Crossover = Crossover,
      Selector = Selector,
      Evaluator = Evaluator,
      Elites = Elites,
      Interceptor = Interceptor,
      Mutator = Mutator,
      MutationRate = MutationRate
    };
  }
}
