namespace HEAL.HeuristicLib.Optimization;

/// <summary>
/// The Pareto relation between two objective vectors, evaluated objective by objective after each objective's
/// direction has been applied.
/// </summary>
/// <remarks>
/// Exactly one member applies to any pair of vectors. <see cref="double.NaN"/> ranks as worse than every other value
/// in both objective directions, so a vector carrying one is dominated rather than dominating.
/// </remarks>
public enum DominanceRelation
{
    /// <summary>
    /// Each vector is better than the other on at least one objective, so neither dominates. Written <c>a ‖ b</c> in
    /// the literature.
    /// </summary>
    Incomparable = 0,

    /// <summary>
    /// Both vectors carry the same value on every objective. Known in the literature as indifference, <c>a ∼ b</c>.
    /// </summary>
    Equal = 1,

    /// <summary>
    /// This vector is not worse than the other on any objective and strictly better on at least one. Written
    /// <c>a ≺ b</c> in the literature.
    /// </summary>
    Dominates = 2,

    /// <summary>
    /// The other vector is not worse than this one on any objective and strictly better on at least one.
    /// </summary>
    IsDominatedBy = 3
}
