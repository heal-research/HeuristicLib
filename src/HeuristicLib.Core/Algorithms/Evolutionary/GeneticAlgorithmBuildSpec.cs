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

public sealed record GeneticAlgorithmBuildSpec<TG, TS, TP>
  : AlgorithmBuildSpec<TG, TS, TP, PopulationState<TG>>,
    ISpecWithCreator<TG, TS, TP>,
    ISpecWithSelector<TG, TS, TP>,
    ISpecWithCrossover<TG, TS, TP>,
    ISpecWithMutator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public int PopulationSize { get; set; }
  public ISelector<TG, TS, TP> Selector { get; set; }
  public ICreator<TG, TS, TP> Creator { get; set; }
  public ICrossover<TG, TS, TP> Crossover { get; set; }
  public IMutator<TG, TS, TP> Mutator { get; set; }
  public double MutationRate { get; set; }
  public int Elites { get; set; }

  public GeneticAlgorithmBuildSpec(
    IEvaluator<TG, TS, TP> Evaluator,
    ITerminator<TG, PopulationState<TG>, TS, TP> Terminator,
    IInterceptor<TG, PopulationState<TG>, TS, TP>? Interceptor,
    IIterationObserver<TG, TS, TP, PopulationState<TG>>? Observer,
    int PopulationSize,
    ISelector<TG, TS, TP> Selector,
    ICreator<TG, TS, TP> Creator,
    ICrossover<TG, TS, TP> Crossover,
    IMutator<TG, TS, TP> Mutator,
    double MutationRate,
    int Elites
  )
    : base(Evaluator, Terminator, Interceptor, Observer)
  {
    this.PopulationSize = PopulationSize;
    this.Selector = Selector;
    this.Creator = Creator;
    this.Crossover = Crossover;
    this.Mutator = Mutator;
    this.MutationRate = MutationRate;
    this.Elites = Elites;
  }
}
