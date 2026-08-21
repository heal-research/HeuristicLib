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
/// The refined candidates are transient. They exist only for the duration of one evaluation, are discarded when it
/// returns, and are never written back into the population, the search state or any algorithm result. The caller keeps
/// the candidates it supplied and pairs them with the returned objective vectors by position, exactly as with every
/// other evaluator.
/// </para>
/// <para>
/// This is Baldwinian refinement: the refinement influences fitness without becoming part of the candidate. Configuring
/// the same refiner as an algorithm's refiner instead makes it Lamarckian, because the algorithm then continues with
/// the refined candidate. The two are different searches and both are expressed by where the refiner is configured.
/// </para>
/// <para>
/// Refiners are generally free to return a differently sized population, but this evaluator is not: it owes its caller
/// one objective vector per supplied candidate. A refiner that changes the population size or order therefore throws
/// here rather than producing a miscounted result.
/// </para>
/// <para>
/// The evaluations issued here belong to <see cref="Evaluator"/>, so counting, limiting and caching attach in the usual
/// way. Passing the same evaluator configuration instance that the algorithm uses resolves to one execution instance,
/// and therefore to one counter and one cache.
/// </para>
/// </remarks>
public sealed record RefinementEvaluator<TCandidate, TSearchSpace, TProblem>
    : Evaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public RefinementEvaluator(IRefiner<TCandidate, TSearchSpace, TProblem> refiner)
    {
        Refiner = refiner;
    }

    /// <summary>
    /// Gets the refiner that produces the transient candidates being measured.
    /// </summary>
    public IRefiner<TCandidate, TSearchSpace, TProblem> Refiner { get; init; }

    /// <summary>
    /// Gets the evaluator that measures the refined candidates.
    /// </summary>
    /// <remarks>
    /// The default is an ordinary <see cref="ProblemEvaluator{TCandidate,TSearchSpace,TProblem}"/>. Because counting,
    /// limiting and caching are wrapper behavior rather than properties of the evaluator role, that default is
    /// unwrapped and therefore invisible to budgets and analysis. Supply the same evaluator instance the algorithm uses
    /// to have these evaluations counted, limited or served from one shared cache.
    /// </remarks>
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } = new ProblemEvaluator<TCandidate, TSearchSpace, TProblem>();

    public override IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(instanceRegistry.Resolve(Evaluator), instanceRegistry.Resolve(Refiner));

    private sealed class Instance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator, IRefinerInstance<TCandidate, TSearchSpace, TProblem> refiner)
        : EvaluatorInstance<TCandidate, TSearchSpace, TProblem>
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
    public static RefinementEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> refiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(refiner);

    public static RefinementEvaluator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> refiner, IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(refiner) { Evaluator = evaluator };
}

public static class RefinementEvaluatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public RefinementEvaluator<TCandidate, TSearchSpace, TProblem> WithRefinement(IRefiner<TCandidate, TSearchSpace, TProblem> refiner) =>
            new RefinementEvaluator<TCandidate, TSearchSpace, TProblem>(refiner) { Evaluator = evaluator };
    }
}
