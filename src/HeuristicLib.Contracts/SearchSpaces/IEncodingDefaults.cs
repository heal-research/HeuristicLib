using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.SearchSpaces;

/// <summary>
/// Marks a search space that states operator defaults, and carries both its own type and its candidate type so a
/// factory can infer them.
/// </summary>
/// <remarks>
/// Search spaces do not declare this directly. Each role interface derives from it, so declaring any role supplies it.
/// </remarks>
public interface IEncodingDefaults<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

/// <summary>
/// Declares the creator a search space suggests when the caller supplies none.
/// </summary>
/// <remarks>
/// Every call must return a new operator. Operators are matched by reference where they anchor an observation, so
/// handing out one shared instance would silently couple algorithms that took the same default.
/// </remarks>
public interface IEncodingDefaultCreator<TCandidate, TSearchSpace> : IEncodingDefaults<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static abstract ICreator<TCandidate> CreateDefaultCreator(TSearchSpace searchSpace);
}

/// <inheritdoc cref="IEncodingDefaultCreator{TCandidate, TSearchSpace}"/>
public interface IEncodingDefaultCrossover<TCandidate, TSearchSpace> : IEncodingDefaults<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static abstract ICrossover<TCandidate> CreateDefaultCrossover(TSearchSpace searchSpace);
}

/// <inheritdoc cref="IEncodingDefaultCreator{TCandidate, TSearchSpace}"/>
public interface IEncodingDefaultMutator<TCandidate, TSearchSpace> : IEncodingDefaults<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static abstract IMutator<TCandidate> CreateDefaultMutator(TSearchSpace searchSpace);
}
