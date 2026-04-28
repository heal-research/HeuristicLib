using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Problems.MetaOptimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

using MetaOptimizationGenotype = CompositeGenotype<RealVector, IntegerVector>;
using MetaOptimizationProblem = IProblem<CompositeGenotype<RealVector, IntegerVector>, CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>>;
using MetaOptimizationSearchSpace = CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>;

public class EmptyMetaOptProblem : MetaOptimizationProblem
{
  public EmptyMetaOptProblem(MetaOptimizationSearchSpace searchSpace)
  {
    SearchSpace = searchSpace;
  }

  public MetaOptimizationSearchSpace SearchSpace { get; }
  public Objective Objective => throw new NotImplementedException();
  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<MetaOptimizationGenotype> genotypes, IRandomNumberGenerator random) => throw new NotImplementedException();
}

public record DynamicRacingAlgorithm<TG, TS, TP, TA, TAlg, TEs> : IterativeAlgorithm<TG, TS, TP, TA, DynamicRacingAlgorithm<TG, TS, TP, TA, TAlg, TEs>.State>
  where TS : class, ISearchSpace<TG>
  where TP : DynamicProblem<TG, TS>
  where TA : PopulationState<TG>
  where TAlg : IterativeAlgorithm<TG, TS, TP, TA, TEs>
  where TEs : IterativeAlgorithm<TG, TS, TP, TA, TEs>.ExecutionState

{
  public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> creator,
                                IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> mutator,
                                Func<TA[], TA> stateMerger,
                                Func<MetaOptimizationGenotype, TAlg> algBuilder)
  {
    MetaSpace = metaSpace;
    Creator = creator;
    Mutator = mutator;
    StateMerger = stateMerger;
    AlgBuilder = algBuilder;
    EmptyMetaOptProblem = new EmptyMetaOptProblem(MetaSpace);
  }

  public Func<TA[], TA> StateMerger { get; } //TODO this could almost be a replacer or an interface
  public ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Creator { get; }
  public IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Mutator { get; }
  private MetaOptimizationSearchSpace MetaSpace { get; }
  private EmptyMetaOptProblem EmptyMetaOptProblem { get; }
  public Func<MetaOptimizationGenotype, TAlg> AlgBuilder { get; }
  public required int NoRacers { get; init; } = 2;

  public class State(DynamicRacingAlgorithm<TG, TS, TP, TA, TAlg, TEs> racer) : ExecutionState
  {
    public MetaOptimizationGenotype? Incumbent { get; set; }
    public required ICreatorInstance<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Creator { get; init; }
    public required IMutatorInstance<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Mutator { get; init; }
  }

  private sealed class Entry : IDisposable
  {
    private IEnumerator<TA> running;
    private readonly IAlgorithm<TG, TS, TP, TA> algorithm;
    public readonly MetaOptimizationGenotype Genotype;
    public TA? LastState { get; private set; }
    private readonly InvocationCounter counter;

    public int UsedCount => counter.CurrentCount;

    public Entry(DynamicRacingAlgorithm<TG, TS, TP, TA, TAlg, TEs> racer, MetaOptimizationGenotype genotype, TP problem,
                 IRandomNumberGenerator random, TA? initialState, CancellationToken ct)
    {
      Genotype = genotype;
      var alg = racer.AlgBuilder(genotype);
      algorithm = alg with { Evaluator = alg.Evaluator.CountInvocations(out counter) };
      running = algorithm.RunStreaming(problem, random, initialState, ct).GetEnumerator();
      LastState = initialState;
    }

    public TA MakeMove(TP problem, IRandomNumberGenerator random, CancellationToken ct)
    {
      var e = running.MoveNext();

      if (!e) {
        running.Dispose();
        running = algorithm.RunStreaming(problem, random, LastState, ct).GetEnumerator();

        if (!running.MoveNext())
          throw new InvalidOperationException("Algorithm is not executable or restartable");
      }

      LastState = running.Current;
      return LastState;
    }

    public void Dispose() => running.Dispose();
  }

  protected override State CreateInitialExecutionState(IExecutionInstanceResolver resolver) => new(this) {
    Evaluator = resolver.Resolve(Evaluator),
    Interceptor = resolver.ResolveOptional(Interceptor),
    Creator = resolver.Resolve(Creator),
    Mutator = resolver.Resolve(Mutator)
  };

  protected override TA ExecuteStep(TA? previousState, State executionState, TP problem, IRandomNumberGenerator random)
  {
    var entries = new List<Entry>(NoRacers);
    executionState.Incumbent ??= executionState.Creator.Create(1, random, MetaSpace, EmptyMetaOptProblem)[0];
    entries.Add(new Entry(this, executionState.Incumbent, problem, random, previousState, CancellationToken.None));
    for (int i = 1; i < NoRacers; i++) {
      var challenger = executionState.Mutator.Mutate([executionState.Incumbent], random, MetaSpace, EmptyMetaOptProblem)[0];
      entries.Add(new Entry(this, challenger, problem, random, previousState, CancellationToken.None));
    }

    problem.EpochClock.OnEpochChange += OnEpochChange;
    bool raceEnded = false;
    while (!raceEnded) {
      var lowest = entries.MinBy(x => x.UsedCount);
      _ = lowest!.MakeMove(problem, random, CancellationToken.None);
      //TODO do fancy calculation with performance prediction that might remove lowest from entries 
    }

    //tidying up
    problem.EpochClock.OnEpochChange -= OnEpochChange;
    foreach (var entry in entries)
      entry.Dispose();

    //set winner
    var best = entries.Select((x, i) => (x.LastState!.Population.Select(solution => solution.ObjectiveVector).Best(problem.Objective), i));
    var winner = best.OrderBy(x => x.Item1, problem.Objective.TotalOrderComparer).First().i;
    executionState.Incumbent = entries[winner].Genotype;

    //merge and return
    return StateMerger(entries.Select(x => x.LastState!).ToArray());

    void OnEpochChange(object? sender, int e) => raceEnded = true;
  }
}
