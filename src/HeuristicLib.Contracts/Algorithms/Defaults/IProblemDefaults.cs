using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>
/// Marks a problem that states operator defaults, and carries its own type so a factory can infer it.
/// </summary>
/// <remarks>
/// This interface declares no roles and never will. Its only job is to give a factory parameter one type that mentions
/// the problem, the candidate and the search space together, which is what lets all three be inferred at a call site.
/// A plain <c>TProblem problem</c> parameter leaves the candidate and search space in constraint position, where
/// inference cannot reach them.
/// <para>
/// Problems do not declare this directly. Each role interface derives from it, so declaring any role supplies it.
/// Which roles an algorithm actually requires is stated by that algorithm's own constraints.
/// </para>
/// </remarks>
public interface IProblemDefaults<TSelf, TCandidate, TSearchSpace> : IProblem<TCandidate, TSearchSpace>
    where TSelf : class
    where TSearchSpace : class, ISearchSpace<TCandidate>;

/// <summary>
/// Declares the creator a problem suggests in place of the one its encoding would supply.
/// </summary>
/// <remarks>
/// Returning <see langword="null"/> delegates the role to the encoding, and is how a problem that an algorithm
/// requires this role from expresses that it has no opinion — either at all, or for this particular instance.
/// <para>
/// Every call must return a new operator, for the reason given on
/// <see cref="IEncodingDefaultCreator{TCandidate, TSearchSpace}"/>.
/// </para>
/// </remarks>
public interface IProblemDefaultCreator<TSelf, TCandidate, TSearchSpace> : IProblemDefaults<TSelf, TCandidate, TSearchSpace>
    where TSelf : class, IProblemDefaultCreator<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static virtual ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? CreateDefaultCreator(TSelf problem) => null;
}

/// <inheritdoc cref="IProblemDefaultCreator{TSelf, TCandidate, TSearchSpace}"/>
public interface IProblemDefaultCrossover<TSelf, TCandidate, TSearchSpace> : IProblemDefaults<TSelf, TCandidate, TSearchSpace>
    where TSelf : class, IProblemDefaultCrossover<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static virtual ICrossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? CreateDefaultCrossover(TSelf problem) => null;
}

/// <inheritdoc cref="IProblemDefaultCreator{TSelf, TCandidate, TSearchSpace}"/>
public interface IProblemDefaultMutator<TSelf, TCandidate, TSearchSpace> : IProblemDefaults<TSelf, TCandidate, TSearchSpace>
    where TSelf : class, IProblemDefaultMutator<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static virtual IMutator<TCandidate>? CreateDefaultMutator(TSelf problem) => null;
}
