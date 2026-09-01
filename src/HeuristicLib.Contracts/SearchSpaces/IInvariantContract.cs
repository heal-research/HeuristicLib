namespace HEAL.HeuristicLib.SearchSpaces;

/// <summary>
/// An operator's declared contract in terms of candidate invariants: what it needs of the candidates handed to it,
/// and which invariants hold of the candidates it produces.
/// </summary>
/// <remarks>
/// The two halves decide the two separate checks that together determine whether an operator may be used over a
/// search space, and they point in opposite directions. Accepting more input makes an operator more widely usable, and
/// ensuring more about its output also makes it more widely usable. An operator that fails one check may pass the
/// other, so neither check substitutes for the other.
/// <para>
/// The output half is a question rather than a list, because the search space asks it. <see cref="Ensures"/> is handed
/// each invariant the space states and answers for that one, which lets an operator answer at whatever level it can:
/// by pattern, when it conforms to a whole family without knowing values, as a mutator preserving the length it was
/// given or a creator taking the length from the space; or by value, when it makes a concrete claim of its own, as a
/// creator generating within bounds it was configured with.
/// </para>
/// <para>
/// Answering is per invariant and optional. Returning <see langword="null"/> states no opinion, and an unanswered
/// invariant is not checked: the search space an operator is typed against already fixes which spaces it may be used
/// over, and that answer stands until a declaration refines it. So an operator can be written without thinking about
/// invariants, adding invariants to a search space never invalidates operators written before them, and answering is
/// worthwhile exactly where the type system is unable to give the right answer.
/// </para>
/// <para>
/// The answer may depend on the operator's own configured values. An operator whose guarantee holds only for part of
/// its parameter range should say so, answering <see langword="true"/> inside that range and <see langword="false"/>
/// outside. Producing a candidate outside the search space is then a declared consequence of a parameter choice rather
/// than a defect, and validation reports it against the configuration that caused it.
/// </para>
/// <para>
/// Such a range is the range in which a guarantee holds. It is <em>not</em> the range of values that are a good
/// choice, and the two must not be conflated. A blend crossover with an alpha above one is a legitimate setting that
/// widens the search; what it would stop doing, without a final clamp, is keeping candidates inside the bounds a
/// search space states. A mutation rate of <c>0.9</c> is unusual advice and breaks no invariant, so it belongs in
/// documentation and never in a contract. Advice about good values stays prose; only guarantees belong here.
/// </para>
/// <para>
/// Where a parameter carries both, document the parameter by pointing at <see cref="Ensures"/> rather than restating
/// the threshold, so the condition is written once in the place that is executed.
/// </para>
/// </remarks>
public interface IInvariantContract<TCandidate>
{
    /// <summary>
    /// Determines whether every candidate this operator produces has <paramref name="invariant"/>. Returns
    /// <see langword="null"/> when this operator states nothing about it, which leaves it unchecked.
    /// </summary>
    /// <remarks>
    /// Ordinarily written as a switch over the invariant, matching by pattern where the operator conforms to a family
    /// and by value where it makes a concrete claim, with a discard arm returning <see langword="null"/> for
    /// everything it has no opinion on.
    /// </remarks>
    bool? Ensures(ISearchInvariant<TCandidate> invariant);

    /// <summary>
    /// Gets the invariants a candidate must have for this operator to accept it. Empty means the operator accepts any
    /// candidate of its representation.
    /// </summary>
    /// <remarks>
    /// A requirement is concrete, because an operator needs a specific thing such as at least two set elements, and it
    /// is satisfied when the search space states an invariant that entails it.
    /// </remarks>
    IReadOnlyList<ISearchInvariant<TCandidate>> RequiredInputInvariants => [];
}
