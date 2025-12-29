using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public static class IterativeAlgorithmExtensions {
  extension<TGenotype, TSearchSpace, TProblem, TIterationState>(
    IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TIterationState> algorithm
  ) 
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TIterationState : IterationState 
  {
    public IEnumerable<TIterationState> ExecuteStreaming(TProblem problem, TSearchSpace? searchSpace = null, TIterationState? previousState = default, IRandomNumberGenerator? random = null) {
      algorithm.CheckSearchSpaceCompatible(problem, searchSpace);
      bool shouldContinue = previousState is null || algorithm.Terminator.ShouldContinue(previousState, previousIterationState: default, searchSpace ?? problem.SearchSpace, problem);

      while (shouldContinue) {
        var newIterationState = algorithm.ExecuteStep(problem, searchSpace ?? problem.SearchSpace, previousState, random ?? algorithm.AlgorithmRandom);
        if (algorithm.Interceptor != null) {
          newIterationState = algorithm.Interceptor.Transform(newIterationState, previousState, searchSpace ?? problem.SearchSpace, problem);
        }
        newIterationState = newIterationState with { CurrentIteration = newIterationState.CurrentIteration + 1 };
        
        yield return newIterationState;
        
        shouldContinue = algorithm.Terminator.ShouldContinue(newIterationState, previousState, searchSpace ?? problem.SearchSpace, problem);
        previousState = newIterationState;
      }
    }

    private void CheckSearchSpaceCompatible(TProblem problem, TSearchSpace? searchSpace) {
      if (searchSpace is ISubSearchSpaceComparable<TSearchSpace> s && !s.IsSubspaceOf(problem.SearchSpace))
        throw new ArgumentException("The provided search space is not a subspace of the problem's search space.");
    }
  }  
}
