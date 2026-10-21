using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// Derive directly from this base when the crossover needs direct control over its execution structure.
/// Use <see cref="StatelessCrossover{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulCrossover{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// <para>
/// The type arguments are the search space and problem this crossover is written for. The base bridges to whatever a
/// run requests, and a request the crossover was not written for is reported when the execution graph is built. A
/// crossover that owns children stays agnostic and derives from <see cref="WrappingOperator{Crossover, TCandidate}"/> or
/// <see cref="MultiOperator{Crossover, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Crossover<TCandidate, TSearchSpace, TProblem>
    : Operator<Crossover, TCandidate, TSearchSpace, TProblem, ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Crossover<TCandidate, TSearchSpace>
    : Crossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Crossover<TCandidate>
    : Crossover<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class CrossoverExecutor<TCandidate, TSearchSpace, TProblem>
    : ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class CrossoverExecutor<TCandidate, TSearchSpace>
    : ICrossoverExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> ICrossoverExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Cross(parents, random, searchSpace);
}

public abstract class CrossoverExecutor<TCandidate>
    : ICrossoverExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> ICrossoverExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Cross(parents, random);
}
