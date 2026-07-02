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
    public ObjectiveDirections Objective => throw new NotImplementedException();
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<MetaOptimizationGenotype> candidates, IRandomNumberGenerator random) => throw new NotImplementedException();
}

public record DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TExecutionState> : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TExecutionState>.State>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : DynamicProblem<TCandidate, TSearchSpace>
  where TSearchState : PopulationState<TCandidate>
  where TAlgorithm : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>
  where TExecutionState : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TExecutionState>.ExecutionState

{
    public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                  ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> creator,
                                  IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> mutator,
                                  Func<TSearchState[], TSearchState> stateMerger,
                                  Func<MetaOptimizationGenotype, TAlgorithm> algBuilder)
    {
        MetaSpace = metaSpace;
        Creator = creator;
        Mutator = mutator;
        StateMerger = stateMerger;
        AlgBuilder = algBuilder;
        EmptyMetaOptProblem = new EmptyMetaOptProblem(MetaSpace);
    }

    public Func<TSearchState[], TSearchState> StateMerger { get; } //TODO this could almost be a replacer or an interface
    public ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Creator { get; }
    public IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Mutator { get; }
    private MetaOptimizationSearchSpace MetaSpace { get; }
    private EmptyMetaOptProblem EmptyMetaOptProblem { get; }
    public Func<MetaOptimizationGenotype, TAlgorithm> AlgBuilder { get; }
    public required int NoRacers { get; init; } = 2;

    public class State(DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TExecutionState> racer) : ExecutionState
    {
        public MetaOptimizationGenotype? Incumbent { get; set; }
        public required ICreatorInstance<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Creator { get; init; }
        public required IMutatorInstance<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Mutator { get; init; }
    }

    private sealed class Entry : IDisposable
    {
        private IEnumerator<TSearchState> running;
        private readonly IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
        public readonly MetaOptimizationGenotype Candidate;
        public TSearchState? LastState { get; private set; }
        private readonly ObservationCounter counter;

        public int UsedCount => counter.CurrentCount;

        public Entry(DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TExecutionState> racer, MetaOptimizationGenotype candidate, TProblem problem,
                     IRandomNumberGenerator random, TSearchState? initialState, CancellationToken ct)
        {
            Candidate = candidate;
            var alg = racer.AlgBuilder(candidate);
            algorithm = alg with { Evaluator = alg.Evaluator.CountEvaluatedGenotypes(out counter) };
            running = algorithm.RunStreaming(problem, random, initialState, ct).GetEnumerator();
            LastState = initialState;
        }

        public TSearchState MakeMove(TProblem problem, IRandomNumberGenerator random, CancellationToken ct)
        {
            var e = running.MoveNext();

            if (!e)
            {
                running.Dispose();
                running = algorithm.RunStreaming(problem, random, LastState, ct).GetEnumerator();

                if (!running.MoveNext())
                    throw new InvalidOperationException("Algorithm cannot start or resume execution");
            }

            LastState = running.Current;
            return LastState;
        }

        public void Dispose() => running.Dispose();
    }

    protected override State CreateInitialExecutionState(IExecutionInstanceResolver resolver) => new(this)
    {
        Evaluator = resolver.Resolve(Evaluator),
        Interceptor = resolver.ResolveOptional(Interceptor),
        Creator = resolver.Resolve(Creator),
        Mutator = resolver.Resolve(Mutator)
    };

    protected override TSearchState ExecuteStep(TSearchState? previousState, State executionState, TProblem problem, IRandomNumberGenerator random)
    {
        var entries = new List<Entry>(NoRacers);
        executionState.Incumbent ??= executionState.Creator.Create(1, random, MetaSpace, EmptyMetaOptProblem)[0];
        entries.Add(new Entry(this, executionState.Incumbent, problem, random, previousState, CancellationToken.None));
        for (int i = 1; i < NoRacers; i++)
        {
            var challenger = executionState.Mutator.Mutate([executionState.Incumbent], random, MetaSpace, EmptyMetaOptProblem)[0];
            entries.Add(new Entry(this, challenger, problem, random, previousState, CancellationToken.None));
        }

        problem.EpochClock.OnEpochChange += OnEpochChange;
        bool raceEnded = false;
        while (!raceEnded)
        {
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
        executionState.Incumbent = entries[winner].Candidate;

        //merge and return
        return StateMerger(entries.Select(x => x.LastState!).ToArray());

        void OnEpochChange(object? sender, int e) => raceEnded = true;
    }
}
