using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record ProblemEvaluator<TCandidate, TSearchSpace, TProblem>
    : StatelessEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate(
        IReadOnlyList<TCandidate> candidates,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem) =>
        problem.Evaluate(candidates, random)
            .Select((objectiveVector, index) => candidates[index].ToEvaluated(objectiveVector))
            .ToArray();
}

public record ProblemEvaluator<TCandidate>
    : ProblemEvaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public static class ProblemEvaluatorExtensions
{
    public static ProblemEvaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
        CreateEvaluator<TCandidate, TSearchSpace>(
            this IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> =>
        new();
}
