using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Reports every mutation to its observers and otherwise delegates to the wrapped mutator.
/// </summary>
/// <remarks>
/// The observers are typed at the search space and problem they were written for, while the mutator itself stays
/// agnostic so it can be used over any run for its candidate type. The two meet when the execution instance is
/// created: observers written for a wider search space or problem accept the run's, and a set written for a narrower
/// one is reported there rather than silently ignored.
/// </remarks>
public sealed record ObservableMutator<TCandidate, TObserverSearchSpace, TObserverProblem>
    : WrappingMutator<TCandidate>
    where TObserverSearchSpace : class, ISearchSpace<TCandidate>
    where TObserverProblem : class, IProblem<TCandidate, TObserverSearchSpace>
{
    public ValueArray<IMutatorObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> Observers { get; init; }

    public ObservableMutator(IMutator<TCandidate> childMutator, params IReadOnlyList<IMutatorObserver<TCandidate, TObserverSearchSpace, TObserverProblem>> observers)
        : base(childMutator)
    {
        Observers = observers.ToValueArray();
    }

    protected override IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childMutator)
    {
        var observers = new IMutatorObserver<TCandidate, TRunSearchSpace, TRunProblem>[Observers.Count];
        for (var i = 0; i < observers.Length; i++)
        {
            if (Observers[i] is not IMutatorObserver<TCandidate, TRunSearchSpace, TRunProblem> observer)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} observes {typeof(TObserverSearchSpace).Name} with {typeof(TObserverProblem).Name}, and cannot observe a run over {typeof(TRunSearchSpace).Name} with {typeof(TRunProblem).Name}.");
            }

            observers[i] = observer;
        }

        return new Instance<TRunSearchSpace, TRunProblem>(childMutator, observers);
    }

    private sealed class Instance<TSearchSpace, TProblem>(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator, IMutatorObserver<TCandidate, TSearchSpace, TProblem>[] observers)
        : WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(childMutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = ChildMutator.Mutate(parents, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterMutate(result, parents, searchSpace, problem);
            }

            return result;
        }
    }
}

public interface IMutatorObserver<TCandidate, in TSearchSpace, in TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterMutate(IReadOnlyList<TCandidate> offspring, IReadOnlyList<TCandidate> parents, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionMutatorObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterMutate)
    : IMutatorObserver<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterMutate(IReadOnlyList<TCandidate> offspring, IReadOnlyList<TCandidate> parents, TSearchSpace searchSpace, TProblem problem) =>
        afterMutate(offspring, parents, searchSpace, problem);
}

public static class ObservableMutator
{
    public static ObservableMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate> childMutator, params IReadOnlyList<IMutatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childMutator, observers);

    public static ObservableMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        IMutator<TCandidate> childMutator,
        Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterMutate)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childMutator, new ActionMutatorObserver<TCandidate, TSearchSpace, TProblem>(afterMutate));

    public static ObservableMutator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> Create<TCandidate>(
        IMutator<TCandidate> childMutator,
        Action<IReadOnlyList<TCandidate>> afterMutate) =>
        new(childMutator, new ActionMutatorObserver<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>((offspring, _, _, _) => afterMutate(offspring)));
}

public static class ObservableMutatorExtensions
{
    extension<TCandidate>(IMutator<TCandidate> mutator)
    {
        public ObservableMutator<TCandidate, TSearchSpace, TProblem> ObserveWith<TSearchSpace, TProblem>(IMutatorObserver<TCandidate, TSearchSpace, TProblem> observer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            new(mutator, observer);

        public ObservableMutator<TCandidate, TSearchSpace, TProblem> ObserveWith<TSearchSpace, TProblem>(params IReadOnlyList<IMutatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            new(mutator, observers);

        public ObservableMutator<TCandidate, TSearchSpace, TProblem> ObserveWith<TSearchSpace, TProblem>(Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterMutate)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            new(mutator, new ActionMutatorObserver<TCandidate, TSearchSpace, TProblem>(afterMutate));

        public ObservableMutator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> ObserveWith(Action<IReadOnlyList<TCandidate>> afterMutate) =>
            ObservableMutator.Create(mutator, afterMutate);
    }
}
