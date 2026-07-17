using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record WrappingCrossover<TCandidate, TSearchSpace, TProblem>
    : Crossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ICrossover<TCandidate, TSearchSpace, TProblem> InnerCrossover { get; }

    protected WrappingCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> innerCrossover)
    {
        InnerCrossover = innerCrossover;
    }

    protected sealed override ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(IExecutionInstanceResolver resolver) =>
        CreateCrossoverInstance(resolver.Resolve(InnerCrossover));

    protected abstract WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> innerCrossover);
}

public abstract class WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> innerCrossover)
    : CrossoverInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ICrossoverInstance<TCandidate, TSearchSpace, TProblem> InnerCrossover { get; } = innerCrossover;
}
