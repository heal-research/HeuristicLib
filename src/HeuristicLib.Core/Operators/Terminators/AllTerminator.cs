using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public partial record AllTerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : MultiTerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public AllTerminator(params ImmutableArray<ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> terminators)
    : base(terminators)
  {
  }

  protected override bool ShouldTerminate(TAlgorithmState algorithmState,
    IReadOnlyList<InnerShouldTerminate> innerTerminators,
    TSearchSpace searchSpace, TProblem problem)
  {
    return innerTerminators.All(t => t(algorithmState, searchSpace, problem));
  }
}
