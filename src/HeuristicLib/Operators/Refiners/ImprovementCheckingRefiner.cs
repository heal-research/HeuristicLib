using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Adds objective-aware retention to another refiner. It evaluates each candidate before and after refinement and keeps
/// whichever one the configured criterion preferred.
/// </summary>
/// <remarks>
/// <para>
/// This is an ordinary refiner. Its contract is still candidate to candidate, and the algorithm evaluates the returned
/// candidate afterwards exactly as it would for any other refiner. A naive configuration therefore performs three
/// problem evaluations per refined candidate: two here and one in the algorithm. That cost is visible and removable —
/// sharing one <see cref="CachingEvaluator{TCandidate,TSearchSpace,TProblem}"/> instance with the algorithm turns the
/// third evaluation into a cache hit.
/// </para>
/// <para>
/// A refiner that cannot improve a candidate returns it unchanged, so its objective vector is unchanged too and the
/// criterion keeps the original. A failed refinement is therefore never turned into a worse candidate.
/// </para>
/// <para>
/// Wrapping order matters and expresses genuinely different searches. An
/// <see cref="IteratedRefiner{TCandidate,TSearchSpace,TProblem}"/> around this refiner is a memetic hill climb that
/// keeps each round only if it improved, while this refiner around an iterated one runs every round and then accepts or
/// rejects the final result once.
/// </para>
/// </remarks>
public sealed record ImprovementCheckingRefiner<TCandidate>
    : IRefiner<TCandidate>
{
    public ImprovementCheckingRefiner(IRefiner<TCandidate> refiner)
    {
        Refiner = refiner;
    }

    /// <summary>
    /// Gets the refiner whose result is accepted or rejected.
    /// </summary>
    public IRefiner<TCandidate> Refiner { get; init; }

    /// <summary>
    /// Gets the evaluator used for both comparison evaluations.
    /// </summary>
    /// <remarks>
    /// The default is an ordinary <see cref="ProblemEvaluator{TCandidate,TSearchSpace,TProblem}"/>. Because counting,
    /// limiting and caching are wrapper behavior rather than properties of the evaluator role, that default is
    /// unwrapped and therefore invisible to budgets and analysis. Supply the same evaluator instance the algorithm uses
    /// to have these evaluations counted, limited or served from one shared cache.
    /// </remarks>
    public IEvaluator<TCandidate> Evaluator { get; init; } = new ProblemEvaluator<TCandidate>();

    /// <summary>
    /// Gets the criterion deciding whether a refined candidate is kept.
    /// </summary>
    /// <remarks>
    /// The default is <see cref="ImprovementChecking.Default"/>, which requires a strictly better objective vector where
    /// the problem defines a total objective order and dominance where it does not. Supplying a criterion lets
    /// acceptance be driven by one dimension of a multi-objective problem, or by a threshold, while selection continues
    /// to use the full objective vector.
    /// </remarks>
    public IImprovementCriterion Criterion { get; init; } = ImprovementChecking.Default;

    public IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return new Instance<TRunSearchSpace, TRunProblem>(resolver.Resolve(Refiner), resolver.Resolve(Evaluator), Criterion);
    }

    private sealed class Instance<TSearchSpace, TProblem>(IRefinerInstance<TCandidate, TSearchSpace, TProblem> refiner, IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator, IImprovementCriterion criterion)
        : RefinerInstance<TCandidate, TSearchSpace, TProblem>
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            if (candidates.Count == 0)
                return candidates;

            var originalObjectiveVectors = evaluator.Evaluate(candidates, random, searchSpace, problem);
            var refined = refiner.Refine(candidates, random, searchSpace, problem);
            if (refined.Count != candidates.Count)
                throw new InvalidOperationException($"The refiner returned {refined.Count} candidates for {candidates.Count} candidates. A refiner must return exactly one candidate for each candidate it receives.");

            var refinedObjectiveVectors = evaluator.Evaluate(refined, random, searchSpace, problem);

            var accepted = new TCandidate[candidates.Count];
            for (var index = 0; index < accepted.Length; index++)
            {
                accepted[index] = criterion.IsImprovement(refinedObjectiveVectors[index], originalObjectiveVectors[index], problem.Objective)
                    ? refined[index]
                    : candidates[index];
            }

            return accepted;
        }
    }
}

public static class ImprovementCheckingRefiner
{
    public static ImprovementCheckingRefiner<TCandidate> Create<TCandidate>(IRefiner<TCandidate> refiner) =>
        new(refiner);

    public static ImprovementCheckingRefiner<TCandidate> Create<TCandidate>(IRefiner<TCandidate> refiner, IImprovementCriterion criterion) =>
        new(refiner) { Criterion = criterion };

    public static ImprovementCheckingRefiner<TCandidate> Create<TCandidate>(IRefiner<TCandidate> refiner, IEvaluator<TCandidate> evaluator) =>
        new(refiner) { Evaluator = evaluator };

    public static ImprovementCheckingRefiner<TCandidate> Create<TCandidate>(IRefiner<TCandidate> refiner, IEvaluator<TCandidate> evaluator, IImprovementCriterion criterion) =>
        new(refiner) { Evaluator = evaluator, Criterion = criterion };
}

/// <summary>
/// Decides whether a refined objective vector counts as an improvement over the objective vector it came from.
/// </summary>
/// <remarks>
/// The two operands are not interchangeable: the criterion answers one directed question rather than defining an
/// ordering. This is why the role is not an <see cref="IComparer{T}"/>. An ordering cannot express a threshold, because
/// moving one operand by a margin makes <c>Compare(a, b)</c> and <c>-Compare(b, a)</c> disagree, and it cannot know
/// which direction to move it in because a comparer does not receive the objective directions.
/// </remarks>
public interface IImprovementCriterion
{
    /// <summary>
    /// Decides whether <paramref name="refined"/> is an improvement over <paramref name="original"/>.
    /// </summary>
    /// <param name="refined">The objective vector measured for the refined candidate.</param>
    /// <param name="original">The objective vector measured for the candidate before refinement.</param>
    /// <param name="objectiveDirections">The problem's objective directions and total objective order.</param>
    bool IsImprovement(ObjectiveVector refined, ObjectiveVector original, ObjectiveDirections objectiveDirections);
}

public static class ImprovementChecking
{
    /// <inheritdoc cref="DefaultImprovementCriterion"/>
    public static DefaultImprovementCriterion Default { get; } = new();

    /// <inheritdoc cref="StrictlyBetterCriterion"/>
    public static StrictlyBetterCriterion StrictlyBetter { get; } = new();

    /// <inheritdoc cref="NotWorseCriterion"/>
    public static NotWorseCriterion NotWorse { get; } = new();

    /// <inheritdoc cref="DominanceCriterion"/>
    public static DominanceCriterion Dominance { get; } = new();

    /// <inheritdoc cref="MinimumImprovementCriterion"/>
    public static MinimumImprovementCriterion MinimumImprovement(double delta) => new(delta);

    /// <inheritdoc cref="MinimumRelativeImprovementCriterion"/>
    public static MinimumRelativeImprovementCriterion MinimumRelativeImprovement(double fraction) => new(fraction);
}

/// <summary>
/// Requires a strictly better objective vector where the problem defines a total objective order, and dominance where
/// it does not.
/// </summary>
/// <remarks>
/// Single-objective problems and multi-objective problems configured with a weighted-sum or lexicographic order all
/// define a total order, so a deliberately configured order decides the comparison. An ordinary Pareto multi-objective
/// problem has no total order and falls back to dominance.
/// </remarks>
public sealed record DefaultImprovementCriterion : IImprovementCriterion
{
    public bool IsImprovement(ObjectiveVector refined, ObjectiveVector original, ObjectiveDirections objectiveDirections) =>
        objectiveDirections.TotalOrderComparer is NoTotalOrderComparer
            ? refined.Dominates(original, objectiveDirections)
            : objectiveDirections.TotalOrderComparer.Compare(refined, original) < 0;
}

/// <summary>
/// Requires a strictly better objective vector according to the problem's total objective order, so an equally good
/// refinement is not an improvement.
/// </summary>
/// <remarks>
/// Throws when the problem defines no total objective order. Use <see cref="DominanceCriterion"/> for ordinary Pareto
/// multi-objective problems.
/// </remarks>
public sealed record StrictlyBetterCriterion : IImprovementCriterion
{
    public bool IsImprovement(ObjectiveVector refined, ObjectiveVector original, ObjectiveDirections objectiveDirections) =>
        objectiveDirections.TotalOrderComparer.Compare(refined, original) < 0;
}

/// <summary>
/// Requires an objective vector that is not worse according to the problem's total objective order, so an equally good
/// refinement is an improvement.
/// </summary>
/// <remarks>
/// Throws when the problem defines no total objective order. Use <see cref="DominanceCriterion"/> for ordinary Pareto
/// multi-objective problems.
/// </remarks>
public sealed record NotWorseCriterion : IImprovementCriterion
{
    public bool IsImprovement(ObjectiveVector refined, ObjectiveVector original, ObjectiveDirections objectiveDirections) =>
        objectiveDirections.TotalOrderComparer.Compare(refined, original) <= 0;
}

/// <summary>
/// Requires the refined objective vector to dominate the original one, so it must be at least as good on every
/// objective and better on at least one. Applies to any number of objectives and needs no total objective order.
/// </summary>
public sealed record DominanceCriterion : IImprovementCriterion
{
    public bool IsImprovement(ObjectiveVector refined, ObjectiveVector original, ObjectiveDirections objectiveDirections) =>
        refined.Dominates(original, objectiveDirections);
}

/// <summary>
/// Requires every objective to improve by at least <see cref="Delta"/> in that objective's own direction.
/// </summary>
/// <remarks>
/// <para>
/// The margin is direction-correct: for a minimized objective it is <c>original - refined</c>, and for a maximized one
/// it is <c>refined - original</c>. A positive <see cref="Delta"/> therefore always means "better by at least this
/// much", regardless of direction.
/// </para>
/// <para>
/// <see cref="Delta"/> is retained exactly as configured and is never clamped. Zero accepts any refinement that is not
/// worse on any objective, and a negative value deliberately tolerates a bounded worsening. A non-finite margin, which
/// arises when either objective value is <see cref="double.NaN"/>, is never an improvement.
/// </para>
/// <para>
/// Every objective must clear the margin, which is a strict requirement for a multi-objective problem. Prefer
/// <see cref="DominanceCriterion"/> when the objectives are meant to trade off against one another.
/// </para>
/// </remarks>
public sealed record MinimumImprovementCriterion(double Delta) : IImprovementCriterion
{
    public bool IsImprovement(ObjectiveVector refined, ObjectiveVector original, ObjectiveDirections objectiveDirections)
    {
        ImprovementMargin.EnsureConsistent(refined, original, objectiveDirections);

        for (var index = 0; index < refined.Count; index++)
        {
            // The negation is required, not stylistic. A NaN margin arises whenever either objective value is NaN, and
            // `!(margin >= Delta)` rejects it while `margin < Delta` would accept it, because every comparison against
            // NaN is false. Rewriting this to the opposite operator would silently make a NaN objective an improvement.
#pragma warning disable S1940 // negated comparison is what rejects a NaN margin
            if (!(ImprovementMargin.Signed(refined[index], original[index], objectiveDirections.Directions[index]) >= Delta))
#pragma warning restore S1940
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Requires every objective to improve by at least <see cref="Fraction"/> of the magnitude of its original value, in
/// that objective's own direction.
/// </summary>
/// <remarks>
/// <para>
/// The required margin for each objective is <c>|original| * Fraction</c>, so the criterion is scale free and
/// direction-correct in the same way as <see cref="MinimumImprovementCriterion"/>.
/// </para>
/// <para>
/// <see cref="Fraction"/> is retained exactly as configured and is never clamped. Where an original value is zero the
/// required margin is zero, so any refinement that is not worse on that objective clears it. Where an original value is
/// infinite the required margin is infinite and no finite improvement clears it. A non-finite margin, which arises when
/// either objective value is <see cref="double.NaN"/>, is never an improvement.
/// </para>
/// </remarks>
public sealed record MinimumRelativeImprovementCriterion(double Fraction) : IImprovementCriterion
{
    public bool IsImprovement(ObjectiveVector refined, ObjectiveVector original, ObjectiveDirections objectiveDirections)
    {
        ImprovementMargin.EnsureConsistent(refined, original, objectiveDirections);

        for (var index = 0; index < refined.Count; index++)
        {
            var required = Math.Abs(original[index]) * Fraction;

            // As in MinimumImprovementCriterion, the negation is what rejects a NaN margin. See the note there before
            // rewriting this to the opposite operator.
#pragma warning disable S1940 // negated comparison is what rejects a NaN margin
            if (!(ImprovementMargin.Signed(refined[index], original[index], objectiveDirections.Directions[index]) >= required))
#pragma warning restore S1940
            {
                return false;
            }
        }

        return true;
    }
}

internal static class ImprovementMargin
{
    /// <summary>
    /// Returns how much better <paramref name="refined"/> is than <paramref name="original"/> for one objective, so a
    /// positive result always means better regardless of the objective's direction.
    /// </summary>
    internal static double Signed(double refined, double original, ObjectiveDirection direction) => direction switch
    {
        ObjectiveDirection.Minimize => original - refined,
        ObjectiveDirection.Maximize => refined - original,
        _ => throw new InvalidOperationException($"Unsupported objective direction: {direction}.")
    };

    internal static void EnsureConsistent(ObjectiveVector refined, ObjectiveVector original, ObjectiveDirections objectiveDirections)
    {
        if (refined.Count != original.Count)
            throw new ArgumentException("Objective values must have the same length");

        if (refined.Count != objectiveDirections.Directions.Length)
            throw new ArgumentException("Objective values and directions must have the same length");
    }
}

public static class ImprovementCheckingRefinerExtensions
{
    extension<TCandidate>(IRefiner<TCandidate> refiner)
    {
        public ImprovementCheckingRefiner<TCandidate> WithImprovementCheck() =>
            new ImprovementCheckingRefiner<TCandidate>(refiner);

        public ImprovementCheckingRefiner<TCandidate> WithImprovementCheck(IImprovementCriterion criterion) =>
            new ImprovementCheckingRefiner<TCandidate>(refiner) { Criterion = criterion };

        public ImprovementCheckingRefiner<TCandidate> WithImprovementCheck(IEvaluator<TCandidate> evaluator) =>
            new ImprovementCheckingRefiner<TCandidate>(refiner) { Evaluator = evaluator };

        public ImprovementCheckingRefiner<TCandidate> WithImprovementCheck(IEvaluator<TCandidate> evaluator, IImprovementCriterion criterion) =>
            new ImprovementCheckingRefiner<TCandidate>(refiner) { Evaluator = evaluator, Criterion = criterion };
    }
}
