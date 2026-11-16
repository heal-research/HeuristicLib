using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <remarks>
/// Derive directly from this base when the crossover needs direct control over its execution structure.
/// Use <see cref="StatelessCrossover{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// <para>
/// The type arguments are the search space and problem this crossover is written for. The base bridges to whatever a
/// run requests, and a request the crossover was not written for is reported when the execution graph is built. A
/// crossover that owns children stays agnostic and derives from <see cref="WrappingOperator{Crossover, TCandidate}"/> or
/// <see cref="MultiOperator{Crossover, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Crossover<TCandidate, TSearchSpace, TProblem>
    : Operator<Crossover, TCandidate, TSearchSpace, TProblem, ICrossoverExecutor<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Crossover<TCandidate, TSearchSpace>
    : Crossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Crossover<TCandidate>
    : Crossover<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class CrossoverExecutor<TCandidate, TSearchSpace, TProblem>
    : ICrossoverExecutor<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected TProblem Problem { get; }
    protected TSearchSpace SearchSpace { get; }

    protected CrossoverExecutor(TProblem problem)
    {
        Problem = problem;
        SearchSpace = problem.SearchSpace;
    }

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random);
}

public abstract class CrossoverExecutor<TCandidate, TSearchSpace>
    : ICrossoverExecutor<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected TSearchSpace SearchSpace { get; }

    protected CrossoverExecutor(TSearchSpace searchSpace)
    {
        SearchSpace = searchSpace;
    }

    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random);
}

public abstract class CrossoverExecutor<TCandidate>
    : ICrossoverExecutor<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random);
}
