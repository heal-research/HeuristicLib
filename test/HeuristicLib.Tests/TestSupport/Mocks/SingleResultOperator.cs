using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

/// <summary>
/// One operator supporting several candidate operations. Every call returns a single result numbering the calls made
/// through its shared prepared state, and the counters record preparation, executor construction and calls.
/// </summary>
public sealed class SingleResultOperator
    : IOperator<Creation, int>,
    IOperator<Crossover, int>,
    IOperator<Mutation, int>,
    IOperator<Selection, int>,
    IOperator<Refinement, int>,
    IOperator<Replacement, int>
{
    public int Preparations { get; private set; }
    public int Constructions { get; private set; }
    public int Invocations { get; private set; }

    // One public method implements every operation's CreateExecutor, whose signature does not name the operation.
    public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<int>
        where TProblem : class, IProblem<int, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        Preparations++;
        Constructions++;
        return new Executor(this);
    }

    private sealed class Executor(SingleResultOperator owner)
        : ICreationExecutor<int>,
        ICrossoverExecutor<int>,
        IMutationExecutor<int>,
        ISelectionExecutor<int>,
        IRefinementExecutor<int>,
        IReplacementExecutor<int>
    {
        private int invocations;

        private int Next()
        {
            owner.Invocations++;
            return ++invocations;
        }

        public IReadOnlyList<int> Create(int count, IRandomNumberGenerator random) => [Next()];
        public IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random) => [Next()];
        public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random) => [Next()];
        public IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random) => [Next()];
        public IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, int count, IRandomNumberGenerator random) => [new(Next(), new ObjectiveVector(0))];
        public IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, int count, IRandomNumberGenerator random) => [new(Next(), new ObjectiveVector(0))];
    }

}
