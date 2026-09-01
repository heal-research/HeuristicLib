using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.SearchSpaces;

/// <summary>
/// Why an operator cannot be used over a search space.
/// </summary>
public enum IncompatibilityReason
{
    /// <summary>The operator does not guarantee an invariant the space requires of its members.</summary>
    OutputMayLeaveTheSearchSpace,

    /// <summary>The operator requires an input invariant the space does not guarantee of its members.</summary>
    InputMayNotBeAccepted
}

/// <summary>
/// One reason an operator and a search space are incompatible, naming the invariant that decided it.
/// </summary>
public sealed record Incompatibility(IncompatibilityReason Reason, string InvariantName, string Explanation)
{
    public override string ToString() => Explanation;
}

/// <summary>
/// Decides whether an operator may be used over a search space, from the invariants both declare.
/// </summary>
/// <remarks>
/// Two checks, because an operator can fail either one alone. The output check asks whether everything the space
/// requires of its members is guaranteed by the operator; the input check asks whether everything the operator
/// requires of its input is guaranteed by the space. An operator that accepts anything but returns candidates outside
/// the space fails only the first, and one that returns valid candidates but rejects members of the space fails only
/// the second.
/// </remarks>
public static class SearchSpaceCompatibility
{
    /// <summary>
    /// Reports every reason <paramref name="candidateOperator"/> cannot be used over <paramref name="searchSpace"/>,
    /// or an empty list when it can.
    /// </summary>
    /// <remarks>
    /// Checking is per invariant. An operator that declares no contract, or answers <see langword="null"/> for an
    /// invariant, is not checked for it: the type system has already decided which search spaces the operator may be
    /// used over, and invariants only refine that answer where a declaration says something the type system cannot.
    /// Declaring is therefore opt in, and adding invariants to a search space never invalidates operators written
    /// before them.
    /// </remarks>
    public static IReadOnlyList<Incompatibility> Check<TCandidate>(IOperator candidateOperator, ISearchSpace<TCandidate> searchSpace)
    {
        if (candidateOperator is not IInvariantContract<TCandidate> contract)
        {
            return [];
        }

        var spaceInvariants = searchSpace.Invariants;
        var required = contract.RequiredInputInvariants;
        var operatorName = candidateOperator.GetType().Name;
        var searchSpaceName = searchSpace.GetType().Name;
        var incompatibilities = new List<Incompatibility>();

        foreach (var spaceInvariant in spaceInvariants)
        {
            if (contract.Ensures(spaceInvariant) == false)
            {
                incompatibilities.Add(new Incompatibility(
                    IncompatibilityReason.OutputMayLeaveTheSearchSpace,
                    spaceInvariant.Name,
                    $"{operatorName} does not ensure {spaceInvariant.Name}, which {searchSpaceName} requires of its members, so its output may leave the search space."));
            }
        }

        foreach (var requiredInvariant in required)
        {
            if (!IsEntailedByAny(spaceInvariants, requiredInvariant))
            {
                incompatibilities.Add(new Incompatibility(
                    IncompatibilityReason.InputMayNotBeAccepted,
                    requiredInvariant.Name,
                    $"{operatorName} requires {requiredInvariant.Name} of its input, which {searchSpaceName} does not guarantee of its members, so the operator may be handed a candidate it cannot accept."));
            }
        }

        return incompatibilities;
    }

    /// <summary>
    /// Determines whether <paramref name="candidateOperator"/> may be used over <paramref name="searchSpace"/>.
    /// </summary>
    public static bool IsCompatible<TCandidate>(IOperator candidateOperator, ISearchSpace<TCandidate> searchSpace) =>
        Check(candidateOperator, searchSpace).Count == 0;

    private static bool IsEntailedByAny<TCandidate>(IReadOnlyList<ISearchInvariant<TCandidate>> available, ISearchInvariant<TCandidate> needed)
    {
        foreach (var invariant in available)
        {
            if (invariant.Entails(needed))
            {
                return true;
            }
        }

        return false;
    }
}
