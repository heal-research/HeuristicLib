using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public partial record AnyTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  : CompositeTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem, NoState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public AnyTerminator(params ImmutableArray<ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> terminators)
    : base(terminators)
  {
  }

  protected override NoState CreateInitialState() => NoState.Instance;
  
  protected override bool ShouldTerminate(TAlgorithmState algorithmState, NoState _,
    IReadOnlyList<ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> innerTerminators,
    TSearchSpace searchSpace, TProblem problem)
  {
    return innerTerminators.Any(t => t.ShouldTerminate(algorithmState, searchSpace, problem));
  }
}
