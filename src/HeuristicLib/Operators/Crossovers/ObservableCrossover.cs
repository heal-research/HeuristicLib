using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public record ObservableCrossover<TCandidate, TSearchSpace, TProblem>
  : WrappingCrossover<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ICrossover<TCandidate, TSearchSpace, TProblem> Crossover => InnerCrossover;

    public ValueArray<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; }

    public ObservableCrossover(ICrossover<TCandidate, TSearchSpace, TProblem> crossover, params IReadOnlyList<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : base(crossover)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> innerCrossover) =>
        new Instance(innerCrossover, Observers);

    private sealed class Instance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> innerCrossover, ValueArray<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(innerCrossover)
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = InnerCrossover.Cross(parents, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterCross(result, parents, searchSpace, problem);
            }

            return result;
        }
    }
}

public interface ICrossoverObserver<TCandidate, in TSearchSpace, in TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterCross(IReadOnlyList<TCandidate> offspring, IReadOnlyList<Parents<TCandidate>> parents, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<TCandidate>, IReadOnlyList<Parents<TCandidate>>, TSearchSpace, TProblem> afterCross) : ICrossoverObserver<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterCross(IReadOnlyList<TCandidate> offspring, IReadOnlyList<Parents<TCandidate>> parents, TSearchSpace searchSpace, TProblem problem) =>
        afterCross(offspring, parents, searchSpace, problem);
}

public static class ObservableCrossoverExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(ICrossoverObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableCrossover<TCandidate, TSearchSpace, TProblem>(crossover, observer);
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<ICrossoverObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableCrossover<TCandidate, TSearchSpace, TProblem>(crossover, observers);
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>, IReadOnlyList<Parents<TCandidate>>, TSearchSpace, TProblem> afterCross) =>
            crossover.ObserveWith(new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>(afterCross));
        public ObservableCrossover<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<TCandidate>> afterCross) =>
            crossover.ObserveWith(new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>((offspring, _, _, _) => afterCross(offspring)));
    }
}
