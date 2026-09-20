using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.SearchSpaces;

/// <summary>
/// Derives the invariant contract of an operator that delegates to other operators, from the contracts of those
/// children.
/// </summary>
/// <remarks>
/// The rule is conservative in both directions, because any child may run and a composition cannot promise more than
/// its weakest part. An invariant survives the composition only when no child breaks it, and the composition requires
/// whatever any child requires.
/// <para>
/// A composition that changes candidates itself, rather than only delegating, must answer for itself instead of using
/// this.
/// </para>
/// </remarks>
public static class OperatorContractComposition
{
    /// <summary>
    /// Answers for a composition from the answers of <paramref name="children"/>: <see langword="false"/> if any child
    /// breaks the invariant, <see langword="null"/> if none breaks it but any child has no opinion, and
    /// <see langword="true"/> when every child ensures it.
    /// </summary>
    public static bool? Ensures<TCandidate>(IEnumerable<IOperator> children, ICandidateInvariant<TCandidate> invariant)
    {
        var anyUnanswered = false;
        var anyChild = false;

        foreach (var child in children)
        {
            anyChild = true;
            var answer = (child as IOperatorContract<TCandidate>)?.Ensures(invariant);
            if (answer == false)
            {
                return false;
            }

            anyUnanswered |= answer is null;
        }

        return anyChild && !anyUnanswered ? true : null;
    }

    /// <summary>
    /// Collects the input requirements of <paramref name="children"/>, without duplicates. Any child may be handed the
    /// composition's input, so the composition requires everything they do.
    /// </summary>
    public static IReadOnlyList<ICandidateInvariant<TCandidate>> Requires<TCandidate>(IEnumerable<IOperator> children)
    {
        var required = new List<ICandidateInvariant<TCandidate>>();
        foreach (var child in children)
        {
            foreach (var requirement in (child as IOperatorContract<TCandidate>)?.Requires ?? [])
            {
                if (!required.Contains(requirement))
                {
                    required.Add(requirement);
                }
            }
        }

        return required;
    }
}
