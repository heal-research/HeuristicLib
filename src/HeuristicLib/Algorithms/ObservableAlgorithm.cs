using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>
/// Wraps an algorithm and notifies observers with every search state the wrapped algorithm yields.
/// </summary>
/// <remarks>
/// The wrapper sits outside the algorithm instance, so it observes exactly what the run streams: the state at the end
/// of an iteration, after any interceptor has transformed it. Sub-iterations an algorithm does not yield are not
/// observed.
/// </remarks>
public sealed record ObservableAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> ChildAlgorithm { get; init; }

    public ValueArray<IAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> Observers { get; init; }

    public ObservableAlgorithm(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm, params IReadOnlyList<IAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
    {
        ChildAlgorithm = childAlgorithm;
        Observers = observers.ToValueArray();
    }

    public IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(instanceRegistry.Resolve(ChildAlgorithm), Observers);

    private sealed class Instance(IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm, ValueArray<IAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        : IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var previousState = initialState;
            await foreach (var state in childAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
            {
                foreach (var observer in observers)
                {
                    observer.AfterIteration(state, previousState, problem.SearchSpace, problem);
                }

                previousState = state;
                yield return state;
            }
        }
    }
}

public static class ObservableAlgorithm
{
    public static ObservableAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm, params IReadOnlyList<IAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childAlgorithm, observers);

    public static ObservableAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm, Action<TSearchState, TSearchState?, TSearchSpace, TProblem> afterIteration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childAlgorithm, new ActionAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterIteration));

    public static ObservableAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm, Action<TSearchState> afterIteration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childAlgorithm, new ActionAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>((state, _, _, _) => afterIteration(state)));
}

/// <summary>
/// Observes the search state at the end of every iteration an algorithm yields.
/// </summary>
public interface IAlgorithmObserver<TCandidate, in TSearchSpace, in TProblem, in TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    void AfterIteration(TSearchState state, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(Action<TSearchState, TSearchState?, TSearchSpace, TProblem> afterIteration)
    : IAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public void AfterIteration(TSearchState state, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem) =>
        afterIteration(state, previousState, searchSpace, problem);
}

public static class ObservableAlgorithmExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public ObservableAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(IAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState> observer) =>
            new(algorithm, observer);
        public ObservableAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(params IReadOnlyList<IAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers) =>
            new(algorithm, observers);
        public ObservableAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(Action<TSearchState, TSearchState?, TSearchSpace, TProblem> afterIteration) =>
            algorithm.ObserveWith(new ActionAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterIteration));
        public ObservableAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> ObserveWith(Action<TSearchState> afterIteration) =>
            algorithm.ObserveWith(new ActionAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>((state, _, _, _) => afterIteration(state)));
    }
}
