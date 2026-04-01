using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

// ToDo: do we really need this interface? I think having the implementation fof the TerminableAlgorithm is enough, and all other algorithms can simply have a Terminator Operator.
public interface ITerminatableAlgorithm<TGenotype, in TSearchSpace, in TProblem, TAlgorithmState>
  : IAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState> Terminator { get; }
}
