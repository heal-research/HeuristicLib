using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Evaluators;

/// <summary>
/// Adapts the score-only problem contract to the evaluator contract by pairing every objective vector with the
/// candidate it was calculated for.
/// </summary>
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
        ProblemEvaluator.Evaluate(candidates, random, problem);
}

public record ProblemEvaluator<TCandidate>
    : ProblemEvaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public static class ProblemEvaluator
{
    public static ProblemEvaluator<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new();

    public static ProblemEvaluator<TCandidate> For<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState => new();

    public static IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate<TCandidate, TSearchSpace>(
        IReadOnlyList<TCandidate> candidates,
        IRandomNumberGenerator random,
        IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
    {
        var objectiveVectors = problem.Evaluate(candidates, random);
        var evaluatedCandidates = new EvaluatedCandidate<TCandidate>[candidates.Count];
        for (var i = 0; i < evaluatedCandidates.Length; i++)
            evaluatedCandidates[i] = candidates[i].ToEvaluated(objectiveVectors[i]);

        return evaluatedCandidates;
    }
}

public static class ProblemEvaluatorExtensions
{
    public static ProblemEvaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
        CreateEvaluator<TCandidate, TSearchSpace>(
            this IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> =>
        new();
}
