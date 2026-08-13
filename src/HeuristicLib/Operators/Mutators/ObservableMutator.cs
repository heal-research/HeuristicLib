using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public sealed record ObservableMutator<TCandidate, TSearchSpace, TProblem>
    : WrappingMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ValueArray<IMutatorObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; }

    public ObservableMutator(IMutator<TCandidate, TSearchSpace, TProblem> childMutator, params IReadOnlyList<IMutatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : base(childMutator)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator) =>
        new Instance(childMutator, Observers);

    private sealed class Instance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator, ValueArray<IMutatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(childMutator)
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
    public static ObservableMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> childMutator, params IReadOnlyList<IMutatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childMutator, observers);

    public static ObservableMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        IMutator<TCandidate, TSearchSpace, TProblem> childMutator,
        Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterMutate)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childMutator, new ActionMutatorObserver<TCandidate, TSearchSpace, TProblem>(afterMutate));

    public static ObservableMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
        IMutator<TCandidate, TSearchSpace, TProblem> childMutator,
        Action<IReadOnlyList<TCandidate>> afterMutate)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childMutator, new ActionMutatorObserver<TCandidate, TSearchSpace, TProblem>((offspring, _, _, _) => afterMutate(offspring)));
}

public static class ObservableMutatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableMutator<TCandidate, TSearchSpace, TProblem> ObserveWith(IMutatorObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableMutator<TCandidate, TSearchSpace, TProblem>(mutator, observer);
        public ObservableMutator<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<IMutatorObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableMutator<TCandidate, TSearchSpace, TProblem>(mutator, observers);
        public ObservableMutator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterMutate) =>
            mutator.ObserveWith(new ActionMutatorObserver<TCandidate, TSearchSpace, TProblem>(afterMutate));
        public ObservableMutator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>> afterMutate) =>
            mutator.ObserveWith(new ActionMutatorObserver<TCandidate, TSearchSpace, TProblem>((offspring, _, _, _) => afterMutate(offspring)));
    }
}
