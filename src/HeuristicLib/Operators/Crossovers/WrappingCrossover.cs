using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record WrappingCrossover<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : ICrossover<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected delegate IReadOnlyList<TCandidate> InnerCross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected ICrossover<TCandidate, TSearchSpace, TProblem> InnerCrossover { get; }

    protected WrappingCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> innerCrossover)
    {
        InnerCrossover = innerCrossover;
    }

    public ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
        var innerCrossover = instanceRegistry.Resolve(InnerCrossover);
        return new Instance(this, CreateInitialState(), innerCrossover.Cross);
    }

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Cross(
      IReadOnlyList<IParents<TCandidate>> parents,
      TExecutionState executionState,
      InnerCross innerCross,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem);

    private sealed class Instance(
      WrappingCrossover<TCandidate, TSearchSpace, TProblem, TExecutionState> wrappingCrossover,
      TExecutionState executionState,
      InnerCross innerCross)
      : ICrossoverInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return wrappingCrossover.Cross(parents, executionState, innerCross, random, searchSpace, problem);
        }
    }
}

public abstract record WrappingCrossover<TCandidate, TSearchSpace, TProblem>
  : WrappingCrossover<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> innerCrossover)
      : base(innerCrossover)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents, NoState executionState,
      InnerCross innerCross, IRandomNumberGenerator random,
      TSearchSpace searchSpace, TProblem problem)
      => Cross(parents, innerCross, random, searchSpace, problem);

    protected abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<IParents<TCandidate>> parents,
      InnerCross innerCross, IRandomNumberGenerator random,
      TSearchSpace searchSpace, TProblem problem);
}
