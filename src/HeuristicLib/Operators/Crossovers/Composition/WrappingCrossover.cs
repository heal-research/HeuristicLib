using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public abstract record WrappingCrossover<TCandidate, TSearchSpace, TProblem>
    : Crossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover)
    {
        ChildCrossover = childCrossover;
    }

    public ICrossover<TCandidate, TSearchSpace, TProblem> ChildCrossover { get; init; }

    public sealed override ICrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance(instanceRegistry.Resolve(ChildCrossover));

    protected abstract WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover);
}

public abstract class WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover)
    : CrossoverInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ICrossoverInstance<TCandidate, TSearchSpace, TProblem> ChildCrossover { get; } = childCrossover;
}
