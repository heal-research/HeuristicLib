using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>
/// Marks a search space that states operator defaults, and carries both its own type and its candidate type so a
/// factory can infer them.
/// </summary>
/// <remarks>
/// This interface declares no roles and never will. Its only job is to give a factory parameter one type that mentions
/// the candidate and the search space together. A plain <c>TSearchSpace searchSpace</c> parameter leaves the candidate
/// in constraint position, where inference cannot reach it.
/// <para>
/// Search spaces do not declare this directly. Each role interface derives from it, so declaring any role supplies it.
/// </para>
/// </remarks>
public interface IEncodingDefaults<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

/// <summary>
/// Declares the creator a search space suggests when the caller supplies none.
/// </summary>
/// <remarks>
/// The member is static so it can never enter a search space record's generated equality, and it takes the search
/// space instance so a suggestion may depend on the values that describe the encoding.
/// <para>
/// Every call must return a new operator. Operators are matched by reference where they anchor an observation, so
/// handing out one shared instance would silently couple algorithms that took the same default.
/// </para>
/// <para>
/// A default is a suggested starting point, not a tuned choice. Changing one changes results for every caller that
/// took it, so treat these as public API.
/// </para>
/// </remarks>
public interface IEncodingDefaultCreator<TCandidate, TSearchSpace> : IEncodingDefaults<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static abstract ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateDefaultCreator(TSearchSpace searchSpace);
}

/// <inheritdoc cref="IEncodingDefaultCreator{TCandidate, TSearchSpace}"/>
public interface IEncodingDefaultCrossover<TCandidate, TSearchSpace> : IEncodingDefaults<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static abstract ICrossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateDefaultCrossover(TSearchSpace searchSpace);
}

/// <inheritdoc cref="IEncodingDefaultCreator{TCandidate, TSearchSpace}"/>
public interface IEncodingDefaultMutator<TCandidate, TSearchSpace> : IEncodingDefaults<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static abstract IMutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateDefaultMutator(TSearchSpace searchSpace);
}
