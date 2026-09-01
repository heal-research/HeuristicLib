using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.SearchSpaces;

/// <summary>
/// Checks that an operator's declared invariant contract matches what it actually does.
/// </summary>
/// <remarks>
/// A declaration is hand written and can be wrong, and compatibility checking believes it. This closes that gap by
/// running the operator over candidates from a search space and confirming that every invariant it claims to ensure
/// still holds afterwards.
/// <para>
/// This is a testing tool, not part of a run. Use it in an operator's own tests, exhaustively where a space is small
/// enough to enumerate and over representative samples otherwise. Passing does not prove the declaration; failing
/// disproves it.
/// </para>
/// </remarks>
public static class InvariantContractVerification
{
    /// <summary>
    /// Applies <paramref name="produce"/> to each sample and reports every invariant the operator answered for that
    /// did not survive, or an empty list when every answer held for every sample.
    /// </summary>
    /// <remarks>
    /// Samples are expected to be members of <paramref name="searchSpace"/>. A sample that is not a member is skipped,
    /// because an operator promises nothing about input it should never receive.
    /// </remarks>
    public static IReadOnlyList<string> Verify<TCandidate>(IOperator candidateOperator, ISearchSpace<TCandidate> searchSpace, IEnumerable<TCandidate> samples, Func<TCandidate, TCandidate> produce)
    {
        var contract = candidateOperator as IInvariantContract<TCandidate>;
        var operatorName = candidateOperator.GetType().Name;

        var claimed = searchSpace.Invariants
            .Where(invariant => contract?.Ensures(invariant) == true)
            .ToArray();

        if (claimed.Length == 0)
        {
            return [];
        }

        var violations = new List<string>();
        foreach (var sample in samples)
        {
            if (!searchSpace.Contains(sample))
            {
                continue;
            }

            var produced = produce(sample);
            foreach (var invariant in claimed)
            {
                if (!invariant.IsSatisfiedBy(produced))
                {
                    violations.Add($"{operatorName} declares that it ensures {invariant.Name}, but produced a candidate without it.");
                }
            }
        }

        return violations.Distinct(StringComparer.Ordinal).ToArray();
    }
}
