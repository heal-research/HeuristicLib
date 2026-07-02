using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
namespace HEAL.HeuristicLib.Operators.Crossovers;

[Equatable]
public abstract partial record MultiCrossover<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : ICrossover<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality] protected ImmutableArray<ICrossover<TCandidate, TSearchSpace, TProblem>> InnerCrossovers { get; }

    protected delegate IReadOnlyList<TCandidate> InnerCross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected MultiCrossover(ImmutableArray<ICrossover<TCandidate, TSearchSpace, TProblem>> innerCrossovers)
    {
        InnerCrossovers = innerCrossovers;
    }

    public ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, InnerCrossovers.Select(instanceRegistry.Resolve).Select(x => (InnerCross)x.Cross).ToArray(), CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, TExecutionState executionState,
      IReadOnlyList<InnerCross> innerCrossovers,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(MultiCrossover<TCandidate, TSearchSpace, TProblem, TExecutionState> multiCrossover, IReadOnlyList<InnerCross> innerCrossovers, TExecutionState executionState)
      : ICrossoverInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return multiCrossover.Cross(parents, executionState, innerCrossovers, random, searchSpace, problem);
        }
    }
}

public abstract record MultiCrossover<TCandidate, TSearchSpace, TProblem>
  : MultiCrossover<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiCrossover(ImmutableArray<ICrossover<TCandidate, TSearchSpace, TProblem>> innerCrossovers)
      : base(innerCrossovers)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, NoState executionState,
      IReadOnlyList<InnerCross> innerCrossovers,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
      => Cross(parents, innerCrossovers, random, searchSpace, problem);

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents,
      IReadOnlyList<InnerCross> innerCrossovers,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
