using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

#pragma warning disable S101
// ReSharper disable once IdentifierTypo
// ReSharper disable once InconsistentNaming
public record NSGA2Builder<TG, TS, TP> : AlgorithmBuilder<TG, TS, TP, PopulationState<TG>, NSGA2<TG, TS, TP>, NSGA2BuildSpec<TG, TS, TP>>
#pragma warning restore S101
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public int PopulationSize { get; set; } = 100;
  public ISelector<TG, TS, TP> Selector { get; set; } = new ParetoCrowdingTournamentSelector<TG>(false, 2);

  public required ICreator<TG, TS, TP> Creator { get; set; }
  public required ICrossover<TG, TS, TP> Crossover { get; set; }
  public required IMutator<TG, TS, TP> Mutator { get; set; }
  public double MutationRate { get; set; } = 0.05;

  public int Elites { get; set; } = 1;

  public override NSGA2BuildSpec<TG, TS, TP> CreateBuildSpec() => new(
    Evaluator, Interceptor, PopulationSize, Selector, Creator, Crossover, Mutator, MutationRate, Elites
  );

  public override NSGA2<TG, TS, TP> BuildFromSpec(NSGA2BuildSpec<TG, TS, TP> spec) => new() {
    PopulationSize = spec.PopulationSize,
    Creator = spec.Creator,
    Crossover = spec.Crossover,
    Selector = spec.Selector,
    Evaluator = spec.Evaluator,
    Replacer = new ElitismReplacer<TG>(spec.Elites),
    Interceptor = spec.Interceptor,
    Mutator = spec.Mutator.WithRate(spec.MutationRate)
  };
}
