using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>,
    IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
  where TState : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>.IterativeAlgorithmState
{
  public IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Interceptor { get; init; }

  protected override async IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(TState state, TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
  {
    var previousState = initialState;

    foreach (var currentIteration in Enumerable.InfiniteSequence(0, 1)) {
      ct.ThrowIfCancellationRequested();
      var iterationRandom = random.Fork(currentIteration);
      var newState = ExecuteStep(state, previousState, problem, iterationRandom);
      if (state.Interceptor is not null) {
        newState = state.Interceptor.Transform(newState, previousState, problem.SearchSpace, problem);
      }

      yield return newState;

      await Task.Yield();

      previousState = newState;
    }
  }

  public abstract class IterativeAlgorithmState : AlgorithmState
  {
    public readonly IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? Interceptor;

    protected IterativeAlgorithmState(ExecutionInstanceRegistry instanceRegistry, IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState> algorithm) : base(instanceRegistry, algorithm)
    {
      Interceptor = algorithm.Interceptor == null ? null : instanceRegistry.Resolve(algorithm.Interceptor);
    }
  }

  protected abstract TAlgorithmState ExecuteStep(TState state, TAlgorithmState? previousState, TProblem problem, IRandomNumberGenerator random);
}

//public abstract record EvolutionaryAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>
//  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>
//  where TSearchSpace : class, ISearchSpace<TGenotype>
//  where TProblem : class, IProblem<TGenotype, TSearchSpace>
//  where TAlgorithmState : class, IAlgorithmState
//  where TState : EvolutionaryAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState>.EvolutionaryAlgorithmState
//{

//  public required int PopulationSize { get; init; }
//  public abstract int OffspringSize { get; }
//  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
//  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
//  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }

//  public abstract class EvolutionaryAlgorithmState : IterativeAlgorithmState
//  {
//    public readonly int PopulationSize;
//    public abstract int OffspringSize { get; }
//    protected readonly ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator;
//    public readonly ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector;
//    public readonly IVariationInstance<TGenotype, TSearchSpace, TProblem> Variator;
//    public readonly IReplacerInstance<TGenotype, TSearchSpace, TProblem> Replacer;

//    public EvolutionaryAlgorithmExecution(
//      ExecutionInstanceRegistry instanceRegistry, EvolutionaryAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState, TState> algorithm) : base(registry, algorithm)
//    {
//      PopulationSize = algorithm.PopulationSize;
//      Creator = instanceRegistry.Resolve(algorithm.Creator);
//      Variator = instanceRegistry.Resolve(algorithm.Mutator);
//      Crossover = instanceRegistry.ResolveOptional(algorithm.Crossover);
//      Selector = instanceRegistry.Resolve(algorithm.Selector);
//    }

//    public override PopulationState<TGenotype> ExecuteStep(PopulationState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
//    {
//      if (previousState is null) {
//        var initialSolutions = Creator.Create(PopulationSize, random, problem.SearchSpace, problem);
//        var initialFitnesses = Evaluator.Evaluate(initialSolutions, random, problem.SearchSpace, problem);
//        return new PopulationState<TGenotype> {
//          Population = Population.From(initialSolutions, initialFitnesses)
//        };
//      }

//      var oldPopulation = previousState.Population.Solutions;

//      var parents = Selector.Select(oldPopulation, problem.Objective, OffspringSize, random, problem.SearchSpace, problem)
//                            .Select(x => x.Genotype).ToList();

//      var offspring = Variator.Alter(parents, random, problem.SearchSpace, problem);

//      var fitnesses = Evaluator.Evaluate(offspring, random, problem.SearchSpace, problem);
//      var offspringPopulation = Population.From(offspring, fitnesses).Solutions;

//      var newPopulation = Replacer.Replace(oldPopulation, offspringPopulation, problem.Objective, PopulationSize, random, problem.SearchSpace, problem);

//      return new PopulationState<TGenotype> {
//        Population = Population.From(newPopulation)
//      };
//    }
//  }
//}
