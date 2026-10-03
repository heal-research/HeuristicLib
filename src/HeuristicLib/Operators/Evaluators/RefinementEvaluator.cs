using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Refines the candidates temporarily, evaluates the refined candidates, and returns those objective vectors for the
/// candidates that were passed in.
/// </summary>
/// <remarks>
/// <para>
/// The refined candidates are transient: they are discarded when the evaluation returns and are never written back
/// into the population, the search state or any algorithm result. This is Baldwinian refinement, as opposed to
/// configuring the refiner on the algorithm, which continues the search with the refined candidate.
/// </para>
/// <para>
/// A refiner that changes the population size or order throws here, because this evaluator owes its caller one
/// objective vector per supplied candidate.
/// </para>
/// <para>
/// The evaluations issued here belong to <see cref="Evaluator"/>, so counting, limiting and caching attach in the usual
/// way. Resolving the same evaluator configuration in a shared scope shares its execution state, including counters and caches.
/// </para>
/// </remarks>
public sealed record RefinementEvaluator<TCandidate>
    : IEvaluator<TCandidate>
{
    public RefinementEvaluator(IRefiner<TCandidate> refiner)
    {
        Refiner = refiner;
    }

    /// <summary>
    /// Gets the refiner that produces the transient candidates being measured.
    /// </summary>
    public IRefiner<TCandidate> Refiner { get; init; }

    /// <summary>
    /// Gets the evaluator that measures the refined candidates.
    /// </summary>
    /// <remarks>
    /// The default is an unwrapped <see cref="ProblemEvaluator{TCandidate,TSearchSpace,TProblem}"/> and is therefore
    /// invisible to budgets and analysis. Supply the same evaluator configuration the algorithm uses to have these
    /// evaluations counted, limited or served from one shared cache.
    /// </remarks>
    public IEvaluator<TCandidate> Evaluator { get; init; } = new ProblemEvaluator<TCandidate>();

    public ExecutionFactory<IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> => scope =>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return new Execution<TRunSearchSpace, TRunProblem>(typed.Resolve(Evaluator), typed.Resolve(Refiner));
    };

    private sealed class Execution<TSearchSpace, TProblem>(IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> evaluator, IRefinerExecution<TCandidate, TSearchSpace, TProblem> refiner)
        : EvaluatorExecution<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var refined = refiner.Refine(candidates, random, searchSpace, problem);

            // Refiners map a population to a population and are free to change its size. This evaluator is not: it owes
            // its caller one objective vector per supplied candidate, in that order, so it needs a refiner that keeps
            // both. Detecting that here names the cause; letting it through would surface as a miscounted or mispaired
            // objective vector in the algorithm, where the refiner is no longer visible.
            if (refined.Count != candidates.Count)
                throw new InvalidOperationException($"The refiner returned {refined.Count} candidates for {candidates.Count} candidates. Transient refinement measures one refined candidate for each supplied candidate, so its refiner must preserve the population size and order.");

            return evaluator.Evaluate(refined, random, searchSpace, problem);
        }
    }
}

public static class RefinementEvaluator
{
    public static RefinementEvaluator<TCandidate> Create<TCandidate>(IRefiner<TCandidate> refiner) =>
        new(refiner);

    public static RefinementEvaluator<TCandidate> Create<TCandidate>(IRefiner<TCandidate> refiner, IEvaluator<TCandidate> evaluator) =>
        new(refiner) { Evaluator = evaluator };
}

public static class RefinementEvaluatorExtensions
{
    extension<TCandidate>(IEvaluator<TCandidate> evaluator)
    {
        public RefinementEvaluator<TCandidate> AppliedAfterRefinement(IRefiner<TCandidate> refiner) =>
            new RefinementEvaluator<TCandidate>(refiner) { Evaluator = evaluator };
    }
}
