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
    [OrderedEquality]
    public ImmutableArray<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; }

    public ObservableCreator(ICreator<TCandidate, TSearchSpace, TProblem> creator, ImmutableArray<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
      : base(creator)
    {
        Observers = observers;
    }

    public ObservableCreator(ICreator<TCandidate, TSearchSpace, TProblem> creator, params IEnumerable<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
      : this(creator, [.. observers])
    {
    }

    protected override IReadOnlyList<TCandidate> Create(int count, InnerCreate innerCreate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        var result = innerCreate(count, random, searchSpace, problem);
        foreach (var observer in Observers)
        {
            observer.AfterCreation(result, count, searchSpace, problem);
        }
        return result;
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
        public ICreator<TCandidate, TSearchSpace, TProblem> ObserveWith(ICreatorObserver<TCandidate, TSearchSpace, TProblem> observer)
          => new ObservableCreator<TCandidate, TSearchSpace, TProblem>(creator, observer);
        public ICreator<TCandidate, TSearchSpace, TProblem> ObserveWith(params IEnumerable<ICreatorObserver<TCandidate, TSearchSpace, TProblem>> observers)
          => new ObservableCreator<TCandidate, TSearchSpace, TProblem>(creator, observers);
        public ICreator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, int, TSearchSpace, TProblem> afterCreation)
          => creator.ObserveWith(new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(afterCreation));
        public ICreator<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>> afterCreation)
          => creator.ObserveWith(new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>((offspring, _, _, _) => afterCreation(offspring)));
    }
}
