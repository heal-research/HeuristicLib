using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

[Equatable]
public partial record ObservableCreator<TCandidate, TSearchSpace, TProblem>
  : WrappingCreator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ICreator<TCandidate, TSearchSpace, TProblem> Creator => InnerCreator;

    [OrderedEquality]
    public ImmutableArray<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; }

    public ObservableCreator(ICreator<TCandidate, TSearchSpace, TProblem> creator, params IReadOnlyList<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : base(creator)
    {
        Observers = observers.ToImmutableArray();
    }

    protected override WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> innerCreator) =>
        new Instance(innerCreator, Observers);

    private sealed class Instance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> innerCreator, ImmutableArray<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingCreatorInstance<TCandidate, TSearchSpace, TProblem>(innerCreator)
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = InnerCreator.Create(count, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterCreation(result, count, searchSpace, problem);
            }

            return result;
        }
    }
}

public interface ICreatorObserver<in TCandidate, in TSearchSpace, in TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterCreation(IReadOnlyList<TCandidate> offspring, int count, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<TCandidate>, int, TSearchSpace, TProblem> afterCreation) : ICreatorObserver<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterCreation(IReadOnlyList<TCandidate> offspring, int count, TSearchSpace searchSpace, TProblem problem) => afterCreation(offspring, count, searchSpace, problem);
}

public static class ObservableCreatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(ICreatorObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableCreator<TCandidate, TSearchSpace, TProblem>(creator, observer);
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableCreator<TCandidate, TSearchSpace, TProblem>(creator, observers);
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, int, TSearchSpace, TProblem> afterCreation) =>
            creator.ObserveWith(new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(afterCreation));
        public ObservableCreator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>> afterCreation) =>
            creator.ObserveWith(new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>((offspring, _, _, _) => afterCreation(offspring)));
    }
}
