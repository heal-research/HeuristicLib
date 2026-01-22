using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public sealed record EvolutionStrategyBuildSpec<TG, TS, TP>
  : AlgorithmBuildSpec<TG, TS, TP, EvolutionStrategyIterationState<TG>>,
    ISpecWithCreator<TG, TS, TP>,
    ISpecWithSelector<TG, TS, TP>,
    ISpecWithMutator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public int PopulationSize { get; set; }
  public EvolutionStrategyType Strategy { get; set; }
  public IMutator<TG, TS, TP> Mutator { get; set; }
  public double InitialMutationStrength { get; set; }
  public ICrossover<TG, TS, TP>? Crossover { get; set; }
  public ISelector<TG, TS, TP> Selector { get; set; }
  public int NumberOfChildren { get; set; }
  public ICreator<TG, TS, TP> Creator { get; set; }

  public EvolutionStrategyBuildSpec(
    IEvaluator<TG, TS, TP> Evaluator,
    ITerminator<TG, EvolutionStrategyIterationState<TG>, TS, TP> Terminator,
    IInterceptor<TG, EvolutionStrategyIterationState<TG>, TS, TP>? Interceptor,
    IIterationObserver<TG, TS, TP, EvolutionStrategyIterationState<TG>>? Observer,
    int PopulationSize,
    EvolutionStrategyType Strategy,
    IMutator<TG, TS, TP> Mutator,
    double InitialMutationStrength,
    ICrossover<TG, TS, TP>? Crossover,
    ISelector<TG, TS, TP> Selector,
    int NumberOfChildren,
    ICreator<TG, TS, TP> Creator
  ) : base(Evaluator, Terminator, Interceptor, Observer) {
    this.PopulationSize = PopulationSize;
    this.Strategy = Strategy;
    this.Mutator = Mutator;
    this.InitialMutationStrength = InitialMutationStrength;
    this.Crossover = Crossover;
    this.Selector = Selector;
    this.NumberOfChildren = NumberOfChildren;
    this.Creator = Creator;
  }
}
