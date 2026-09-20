using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Evaluates candidates directly through the problem. This is the ordinary evaluator and the innermost child of every
/// evaluator composition.
/// </summary>
public record ProblemEvaluator<TCandidate, TSearchSpace, TProblem>
    : StatelessEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
        ProblemEvaluator.Evaluate(candidates, random, problem);
}

public record ProblemEvaluator<TCandidate>
    : ProblemEvaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public static class ProblemEvaluator
{
    public static ProblemEvaluator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => new();


    public static IReadOnlyList<ObjectiveVector> Evaluate<TCandidate, TSearchSpace>(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> =>
        problem.Evaluate(candidates, random);
}

public static class ProblemEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
    {
        public ProblemEvaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateEvaluator() => new();
    }
}
