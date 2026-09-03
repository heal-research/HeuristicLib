using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Observation happens on the state as the wrapped algorithm yields it at the end of an iteration, after any
/// interceptor has transformed it. Sub-iterations an algorithm does not yield are not observed. A run the observers
/// were not written for is reported when the execution graph is built.
/// </remarks>
public sealed record ObservableAlgorithm<TCandidate, TObserverSearchSpace, TObserverProblem, TObserverSearchState>
    : Algorithm<ObservableAlgorithm<TCandidate, TObserverSearchSpace, TObserverProblem, TObserverSearchState>, TCandidate, TObserverSearchState>
    where TObserverSearchSpace : class, ISearchSpace<TCandidate>
    where TObserverProblem : class, IProblem<TCandidate, TObserverSearchSpace>
    where TObserverSearchState : class, ISearchState
{
    public IAlgorithm<TCandidate, TObserverSearchState> ChildAlgorithm { get; init; }

    public ValueArray<IAlgorithmObserver<TCandidate, TObserverSearchSpace, TObserverProblem, TObserverSearchState>> Observers { get; init; }

    public ObservableAlgorithm(IAlgorithm<TCandidate, TObserverSearchState> childAlgorithm, params IReadOnlyList<IAlgorithmObserver<TCandidate, TObserverSearchSpace, TObserverProblem, TObserverSearchState>> observers)
    {
        ChildAlgorithm = childAlgorithm;
        Observers = observers.ToValueArray();
    }

    public override IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TObserverSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        var observers = new IAlgorithmObserver<TCandidate, TRunSearchSpace, TRunProblem, TObserverSearchState>[Observers.Count];
        for (var i = 0; i < observers.Length; i++)
        {
            if (Observers[i] is not IAlgorithmObserver<TCandidate, TRunSearchSpace, TRunProblem, TObserverSearchState> observer)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} observes {typeof(TObserverSearchSpace).Name} with {typeof(TObserverProblem).Name}, and cannot observe a run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
            }

            observers[i] = observer;
        }

        return new Instance<TRunSearchSpace, TRunProblem, TObserverSearchState>(
            instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TObserverSearchState>(ChildAlgorithm), observers);
    }

    private sealed class Instance<TSearchSpace, TProblem, TSearchState>(
        IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm,
        IAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>[] observers)
        : IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
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
    public static ObservableAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IAlgorithm<TCandidate, TSearchState> childAlgorithm, params IReadOnlyList<IAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childAlgorithm, observers);

    public static ObservableAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IAlgorithm<TCandidate, TSearchState> childAlgorithm, Action<TSearchState, TSearchState?, TSearchSpace, TProblem> afterIteration)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new(childAlgorithm, new ActionAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterIteration));

    /// <summary>Observes the state only, so the observer is written at the widest search space and problem.</summary>
    public static ObservableAlgorithm<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> Create<TCandidate, TSearchState>(
        IAlgorithm<TCandidate, TSearchState> childAlgorithm, Action<TSearchState> afterIteration)
        where TSearchState : class, ISearchState =>
        new(childAlgorithm, new ActionAlgorithmObserver<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>((state, _, _, _) => afterIteration(state)));
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
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
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
    }

    /// <remarks>On the authoring base so the lambda's state parameter is typed from the receiver.</remarks>
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public ObservableAlgorithm<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> ObserveWith(Action<TSearchState> afterIteration) =>
            ObservableAlgorithm.Create(algorithm, afterIteration);
    }
}
