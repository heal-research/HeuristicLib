namespace HEAL.HeuristicLib.SearchSpaces;

/// <summary>
/// A named property that a candidate either has or does not have, such as a length or a number of set elements.
/// Search spaces state the invariants their members satisfy, and operators state the invariants they require of their
/// input and guarantee of their output.
/// </summary>
/// <remarks>
/// Implementations should be immutable value types, ordinarily records, so that equality is by value. Any type may
/// declare a new invariant; nothing in the library enumerates them.
/// </remarks>
public interface ISearchInvariant<TCandidate>
{
    /// <summary>
    /// Gets a short human readable name used in diagnostics, such as <c>Length(4)</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether <paramref name="candidate"/> has this property. Compatibility checking does not call this;
    /// it exists for diagnostics and for verifying that a declared invariant matches actual behavior.
    /// </summary>
    bool IsSatisfiedBy(TCandidate candidate);

    /// <summary>
    /// Determines whether holding this invariant implies holding <paramref name="other"/>. The default is value
    /// equality. Override where one invariant is strictly stronger than another, as an exact count entails a minimum
    /// count.
    /// </summary>
    bool Entails(ISearchInvariant<TCandidate> other) => Equals(other);
}
