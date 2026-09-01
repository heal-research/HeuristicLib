namespace HEAL.HeuristicLib.SearchSpaces;

/// <summary>
/// A named property that a candidate either has or does not have, such as a length or a number of set elements.
/// Search spaces state the invariants their members satisfy, and operators state the invariants they require of their
/// input and guarantee of their output.
/// </summary>
/// <remarks>
/// Invariants exist because C# cannot express the property that decides whether an operator may be used over a search
/// space. Two search spaces over one candidate representation are two subsets of a single type, not two types, and
/// variance relates types rather than subsets of a type. Whether an operator keeps candidates inside a subset is a
/// postcondition on its output, and a postcondition is not a type, so the check is declared here and verified at run
/// time instead.
/// <para>
/// Implementations should be immutable value types, ordinarily records, so that equality is by value. Any type may
/// declare a new invariant; nothing in the library enumerates them. The candidate type is invariant here because
/// <see cref="Entails"/> takes another invariant over the same candidate.
/// </para>
/// </remarks>
public interface ISearchInvariant<TCandidate>
{
    /// <summary>
    /// Gets a short human readable name used in diagnostics, such as <c>Length(4)</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether <paramref name="candidate"/> has this property.
    /// </summary>
    /// <remarks>
    /// Compatibility checking does not call this. It exists for diagnostics and for verifying that a declared
    /// invariant matches actual behavior.
    /// </remarks>
    bool IsSatisfiedBy(TCandidate candidate);

    /// <summary>
    /// Determines whether holding this invariant implies holding <paramref name="other"/>. The default is value
    /// equality.
    /// </summary>
    /// <remarks>
    /// Override this where one invariant is strictly stronger than another. A candidate with exactly two set elements
    /// also has at least two set elements, so the exact-count invariant entails the minimum-count invariant, and an
    /// operator requiring the weaker one may be used over a space guaranteeing the stronger one.
    /// </remarks>
    bool Entails(ISearchInvariant<TCandidate> other) => Equals(other);
}
