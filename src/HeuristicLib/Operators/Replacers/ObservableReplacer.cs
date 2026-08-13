using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

public record ObservableReplacer<TCandidate, TSearchSpace, TProblem>
  : WrappingReplacer<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IReplacer<TCandidate, TSearchSpace, TProblem> Replacer => InnerReplacer;

    public ValueArray<IReplacerObserver<TCandidate, TSearchSpace, TProblem>> Observers { get; init; }

    public ObservableReplacer(IReplacer<TCandidate, TSearchSpace, TProblem> replacer, params IReadOnlyList<IReplacerObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : base(replacer)
    {
        Observers = observers.ToValueArray();
    }

    protected override WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateReplacerInstance(IReplacerInstance<TCandidate, TSearchSpace, TProblem> innerReplacer) =>
        new Instance(innerReplacer, Observers);

    private sealed class Instance(IReplacerInstance<TCandidate, TSearchSpace, TProblem> innerReplacer, ValueArray<IReplacerObserver<TCandidate, TSearchSpace, TProblem>> observers)
        : WrappingReplacerInstance<TCandidate, TSearchSpace, TProblem>(innerReplacer)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var result = InnerReplacer.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
            foreach (var observer in observers)
            {
                observer.AfterReplacement(result, previousPopulation, offspringPopulation, objective, searchSpace, problem);
            }

            return result;
        }
    }
}

public interface IReplacerObserver<TCandidate, in TSearchSpace, in TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    void AfterReplacement(IReadOnlyList<EvaluatedCandidate<TCandidate>> newPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, TSearchSpace searchSpace, TProblem problem);
}

public sealed class ActionReplacerObserver<TCandidate, TSearchSpace, TProblem>(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, TSearchSpace, TProblem> afterReplacement)
    : IReplacerObserver<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public void AfterReplacement(IReadOnlyList<EvaluatedCandidate<TCandidate>> newPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, TSearchSpace searchSpace, TProblem problem) =>
        afterReplacement(newPopulation, previousPopulation, offspringPopulation, objective, searchSpace, problem);
}

public static class ObservableReplacerExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ObservableReplacer<TCandidate, TSearchSpace, TProblem> ObserveWith(IReplacerObserver<TCandidate, TSearchSpace, TProblem> observer) =>
            new ObservableReplacer<TCandidate, TSearchSpace, TProblem>(replacer, observer);
        public ObservableReplacer<TCandidate, TSearchSpace, TProblem> ObserveWith(params IReadOnlyList<IReplacerObserver<TCandidate, TSearchSpace, TProblem>> observers) =>
            new ObservableReplacer<TCandidate, TSearchSpace, TProblem>(replacer, observers);
        public ObservableReplacer<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, TSearchSpace, TProblem> afterReplacement) =>
            replacer.ObserveWith(new ActionReplacerObserver<TCandidate, TSearchSpace, TProblem>((newPopulation, previousPopulation, offspringPopulation, _, searchSpace, problem) => afterReplacement(newPopulation, previousPopulation, offspringPopulation, searchSpace, problem)));
        public ObservableReplacer<TCandidate, TSearchSpace, TProblem> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>> afterReplacement) =>
            replacer.ObserveWith(new ActionReplacerObserver<TCandidate, TSearchSpace, TProblem>((newPopulation, _, _, _, _, _) => afterReplacement(newPopulation)));
    }
}
