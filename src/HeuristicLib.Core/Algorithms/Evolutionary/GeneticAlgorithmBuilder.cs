using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record GeneticAlgorithmBuilder<TG, TS, TP>
  : AlgorithmBuilder<TG, TS, TP, PopulationState<TG>, GeneticAlgorithm<TG, TS, TP>, GeneticAlgorithmBuildSpec<TG, TS, TP>>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TG : class
{
  public int PopulationSize { get; set; } = 100;
  public ISelector<TG, TS, TP> Selector { get; set; } = new TournamentSelector<TG>(2);

  public required ICreator<TG, TS, TP> Creator { get; set; }
  public required ICrossover<TG, TS, TP> Crossover { get; set; }
  public required IMutator<TG, TS, TP> Mutator { get; set; }
  public double MutationRate { get; set; } = 0.05;

  //public ITerminator<TG, PopulationIterationState<TG>, TS, TP> Terminator { get; set; } = new AfterIterationsTerminator<TG>(100);

  public int Elites { get; set; } = 1;

  public override GeneticAlgorithmBuildSpec<TG, TS, TP> CreateBuildSpec() => new(
    Evaluator, Terminator, Interceptor, Observer, PopulationSize, Selector, Creator, Crossover, Mutator, MutationRate, Elites
  );

  public override GeneticAlgorithm<TG, TS, TP> BuildFromSpec(GeneticAlgorithmBuildSpec<TG, TS, TP> spec)
  {
    // ToDo: how to prevent accidentally reading from the builder instead of the spec here?
    return new GeneticAlgorithm<TG, TS, TP> {
      PopulationSize = spec.PopulationSize,
      Creator = spec.Creator,
      Crossover = spec.Crossover,
      Selector = spec.Selector,
      Evaluator = spec.Evaluator,
      Replacer = new ElitismReplacer<TG>(spec.Elites),
      //Terminator = spec.Terminator,
      Interceptor = spec.Interceptor,
      Mutator = spec.Mutator.WithRate(spec.MutationRate)
    };
  }
}
