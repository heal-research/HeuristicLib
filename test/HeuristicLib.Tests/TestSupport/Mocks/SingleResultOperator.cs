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
    public int Calls { get; private set; }

    // One public method implements every operation's CreateExecutor, whose signature does not name the operation.
    public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<int>
        where TProblem : class, IProblem<int, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        var state = scope.GetScopedState(this, () =>
        {
            Preparations++;
            return new State();
        });
        Constructions++;
        return new Executor(this, state);
    }


    private sealed class State
    {
        public int Calls { get; set; }
    }

    private sealed class Executor(SingleResultOperator owner, State state)
        : ICreationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>,
        ICrossoverExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>,
        IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>,
        ISelectionExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>,
        IRefinementExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>,
        IReplacementExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
    {
        private int Next()
        {
            owner.Calls++;
            return ++state.Calls;
        }

        public IReadOnlyList<int> Create(int count, IRandomNumberGenerator random, ISearchSpace<int> searchSpace, IProblem<int, ISearchSpace<int>> problem) => [Next()];
        public IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random, ISearchSpace<int> searchSpace, IProblem<int, ISearchSpace<int>> problem) => [Next()];
        public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, ISearchSpace<int> searchSpace, IProblem<int, ISearchSpace<int>> problem) => [Next()];
        public IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random, ISearchSpace<int> searchSpace, IProblem<int, ISearchSpace<int>> problem) => [Next()];
        public IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<int> searchSpace, IProblem<int, ISearchSpace<int>> problem) => [new(Next(), new ObjectiveVector(0))];
        public IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<int> searchSpace, IProblem<int, ISearchSpace<int>> problem) => [new(Next(), new ObjectiveVector(0))];
    }
}
