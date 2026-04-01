using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record DecoratorTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, TState>
  : ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState> InnerTerminator { get; }
  
  protected DecoratorTerminator(ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerTerminator)
  {
    InnerTerminator = innerTerminator;
  }
  
  public ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
    new Instance(this, instanceRegistry.Resolve(InnerTerminator), CreateInitialState());
  
  protected abstract TState CreateInitialState();
  
  protected abstract bool ShouldTerminate(TAlgorithmState algorithmState, TState state,
    ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerTerminator,
    TSearchSpace searchSpace, TProblem problem);
  
  private sealed class Instance(DecoratorTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, TState> decorator,
    ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerTerminator, TState terminatorState)
    : ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  {
    public bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem)
    {
      return decorator.ShouldTerminate(state, terminatorState, innerTerminator, searchSpace, problem);
    }
  }
}

public abstract record DecoratorTerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : DecoratorTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, NoState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected DecoratorTerminator(ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerTerminator)
    : base(innerTerminator)
  {
  }

  protected sealed override NoState CreateInitialState() => NoState.Instance;

  protected sealed override bool ShouldTerminate(TAlgorithmState algorithmState, NoState state,
    ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerTerminator,
    TSearchSpace searchSpace, TProblem problem)
    => ShouldTerminate(algorithmState, innerTerminator, searchSpace, problem);

  protected abstract bool ShouldTerminate(TAlgorithmState algorithmState,
    ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> innerTerminator,
    TSearchSpace searchSpace, TProblem problem);
}
