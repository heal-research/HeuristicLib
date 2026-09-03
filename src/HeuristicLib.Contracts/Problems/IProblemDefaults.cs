using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

/// <summary>
/// Declares the creator a problem suggests in place of the one its encoding would supply.
/// </summary>
/// <remarks>
/// Returning <see langword="null"/> delegates the role to the encoding, and is how a problem that an algorithm
/// requires this role from expresses that it has no opinion — either at all, or for this particular instance.
/// <para>
/// Every call must return a new operator, for the reason given on
/// <see cref="IEncodingDefaultCreator{TCandidate,TSearchSpace}"/>.
/// </para>
/// </remarks>
public interface IProblemDefaultCreator<TSelf, TCandidate, TSearchSpace> : IProblem<TCandidate, TSearchSpace>
    where TSelf : class, IProblemDefaultCreator<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static virtual ICreator<TCandidate>? CreateDefaultCreator(TSelf problem) => null;
}

/// <inheritdoc cref="IProblemDefaultCreator{TSelf, TCandidate, TSearchSpace}"/>
public interface IProblemDefaultCrossover<TSelf, TCandidate, TSearchSpace> : IProblem<TCandidate, TSearchSpace>
    where TSelf : class, IProblemDefaultCrossover<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static virtual ICrossover<TCandidate>? CreateDefaultCrossover(TSelf problem) => null;
}

/// <inheritdoc cref="IProblemDefaultCreator{TSelf, TCandidate, TSearchSpace}"/>
public interface IProblemDefaultMutator<TSelf, TCandidate, TSearchSpace> : IProblem<TCandidate, TSearchSpace>
    where TSelf : class, IProblemDefaultMutator<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static virtual IMutator<TCandidate>? CreateDefaultMutator(TSelf problem) => null;
}
