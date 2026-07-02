using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

public abstract record WrappingCreator<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : ICreator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected delegate IReadOnlyList<TCandidate> InnerCreate(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected ICreator<TCandidate, TSearchSpace, TProblem> InnerCreator { get; }

    protected WrappingCreator(ICreator<TCandidate, TSearchSpace, TProblem> innerCreator)
    {
        InnerCreator = innerCreator;
    }

    public ICreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
        var innerCreator = instanceRegistry.Resolve(InnerCreator);
        return new Instance(this, CreateInitialState(), innerCreator.Create);
    }

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<TCandidate> Create(
      int count,
      TExecutionState executionState,
      InnerCreate innerCreate,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem);

    private sealed class Instance(
      WrappingCreator<TCandidate, TSearchSpace, TProblem, TExecutionState> wrappingCreator,
      TExecutionState executionState,
      InnerCreate innerCreate)
      : ICreatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return wrappingCreator.Create(count, executionState, innerCreate, random, searchSpace, problem);
        }
    }
}

public abstract record WrappingCreator<TCandidate, TSearchSpace, TProblem>
  : WrappingCreator<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingCreator(ICreator<TCandidate, TSearchSpace, TProblem> innerCreator)
      : base(innerCreator)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<TCandidate> Create(int count, NoState executionState,
      InnerCreate innerCreate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
      => Create(count, innerCreate, random, searchSpace, problem);

    protected abstract IReadOnlyList<TCandidate> Create(int count, InnerCreate innerCreate,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
