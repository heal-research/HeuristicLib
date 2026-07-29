using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record DirectEvaluator<TCandidate>
  : StatelessEvaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
      => DirectEvaluator.Evaluate(candidates, random, problem);
}

public static class DirectEvaluator
{
    public static DirectEvaluator<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem) where TSearchSpace : class, ISearchSpace<TCandidate> => new();

    public static DirectEvaluator<TCandidate> For<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState => new();

    public static IReadOnlyList<ObjectiveVector> Evaluate<TCandidate>(
      IReadOnlyList<TCandidate> candidates,
      IRandomNumberGenerator random,
      IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => problem.Evaluate(candidates, random);
}
