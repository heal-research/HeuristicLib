using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record NSGA2Builder<TG, TS, TP> : AlgorithmBuilder<TG, TS, TP, PopulationIterationState<TG>, NSGA2<TG, TS, TP>> 
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TG : class
{
  public int PopulationSize { get; set; } = 100;
  public ISelector<TG, TS, TP> Selector { get; set; } = new ParetoCrowdingTournamentSelector<TG>(dominateOnEqualities: false, tournamentSize: 2);
  
  public required ICreator<TG, TS, TP> Creator { get; set; }
  public required ICrossover<TG, TS, TP> Crossover { get; set; }
  public required IMutator<TG, TS, TP> Mutator { get; set; }
  public double MutationRate { get; set; } = 0.05;
  
  public int Elites { get; set; } = 1;

  public override NSGA2<TG, TS, TP> Build() => new() {
    PopulationSize = PopulationSize,
    Creator = Creator,
    Crossover = Crossover,
    Selector = Selector,
    Evaluator = Evaluator,
    Replacer = new ElitismReplacer<TG>(Elites),
    Terminator = Terminator,
    Interceptor = Interceptor,
    Mutator = Mutator.WithRate(MutationRate)
  };
  
}
