using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record DirectEvaluator<TCandidate>
  : StatelessEvaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
      => DirectEvaluator.Evaluate(candidates, random, problem);
}

public static class DirectEvaluator
{
    public static IReadOnlyList<ObjectiveVector> Evaluate<TCandidate>(
      IReadOnlyList<TCandidate> candidates,
      IRandomNumberGenerator random,
      IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => problem.Evaluate(candidates, random);
}

public static class DirectEvaluatorExtensions
{
    public static DirectEvaluator<TCandidate> CreateEvaluator<TCandidate>(this IProblem<TCandidate, ISearchSpace<TCandidate>> algorithm) => new();
}
