namespace HEAL.HeuristicLib.Execution;

/// <summary>Substitutions that belong together and apply in order, such as the placement of a trace, a counter or a single <see cref="Substitution"/>.</summary>
/// <remarks>
/// <c>Substitute</c> takes bundles, so one call can mix traces, counters and plain substitutions, and <c>AnalyzedBy</c> is
/// the same call under the name of its most common use. A bundle is flat: a call applies the substitutions of all its
/// bundles one after another. The list must return the same instances on every read, because a substitution listed twice
/// in one call applies once.
/// </remarks>
public interface ISubstitutionBundle
{
    /// <summary>The substitutions of this bundle, in the order a call applies them.</summary>
    IReadOnlyList<Substitution> Substitutions { get; }
}
