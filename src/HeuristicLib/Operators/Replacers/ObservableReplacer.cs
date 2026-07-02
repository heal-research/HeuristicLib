using Generator.Equals;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

[Equatable]
public partial record ObservableReplacer<TG, TS, TP>
  : WrappingReplacer<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
    [OrderedEquality]
    public ImmutableArray<IReplacerObserver<TG, TS, TP>> Observers { get; }

    public ObservableReplacer(IReplacer<TG, TS, TP> replacer, ImmutableArray<IReplacerObserver<TG, TS, TP>> observers)
      : base(replacer)
    {
        Observers = observers;
    }

    public ObservableReplacer(IReplacer<TG, TS, TP> replacer, params IEnumerable<IReplacerObserver<TG, TS, TP>> observers)
      : this(replacer, [.. observers])
    {
    }

    protected override IReadOnlyList<EvaluatedCandidate<TG>> Replace(IReadOnlyList<EvaluatedCandidate<TG>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TG>> offspringPopulation, ObjectiveDirections objective, int count, InnerReplace innerReplace, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
        var result = innerReplace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        foreach (var observer in Observers)
        {
            observer.AfterReplacement(result, previousPopulation, offspringPopulation, objective, searchSpace, problem);
        }
        return result;
    }
}

public interface IReplacerObserver<TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
    void AfterReplacement(IReadOnlyList<EvaluatedCandidate<TG>> newPopulation, IReadOnlyList<EvaluatedCandidate<TG>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TG>> offspringPopulation, ObjectiveDirections objective, TS searchSpace, TP problem);
}

public static class ObservableReplacerExtensions
{
    extension<TG, TS, TP>(IReplacer<TG, TS, TP> replacer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
    {
        public IReplacer<TG, TS, TP> ObserveWith(IReplacerObserver<TG, TS, TP> observer)
          => new ObservableReplacer<TG, TS, TP>(replacer, observer);
        public IReplacer<TG, TS, TP> ObserveWith(params IEnumerable<IReplacerObserver<TG, TS, TP>> observers)
          => new ObservableReplacer<TG, TS, TP>(replacer, observers);
        public IReplacer<TG, TS, TP> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TG>>, IReadOnlyList<EvaluatedCandidate<TG>>, IReadOnlyList<EvaluatedCandidate<TG>>, TS, TP> afterReplacement)
          => replacer.ObserveWith(new ActionReplacerObserver<TG, TS, TP>((newPopulation, previousPopulation, offspringPopulation, _, searchSpace, problem)
            => afterReplacement(newPopulation, previousPopulation, offspringPopulation, searchSpace, problem)));
        public IReplacer<TG, TS, TP> ObserveWith(Action<IReadOnlyList<EvaluatedCandidate<TG>>> afterReplacement)
          => replacer.ObserveWith(new ActionReplacerObserver<TG, TS, TP>((newPopulation, _, _, _, _, _) => afterReplacement(newPopulation)));
    }
}

public sealed class ActionReplacerObserver<TG, TS, TP>(Action<IReadOnlyList<EvaluatedCandidate<TG>>, IReadOnlyList<EvaluatedCandidate<TG>>, IReadOnlyList<EvaluatedCandidate<TG>>, ObjectiveDirections, TS, TP> afterReplacement) : IReplacerObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
    public void AfterReplacement(IReadOnlyList<EvaluatedCandidate<TG>> newPopulation, IReadOnlyList<EvaluatedCandidate<TG>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TG>> offspringPopulation, ObjectiveDirections objective, TS searchSpace, TP problem)
      => afterReplacement(newPopulation, previousPopulation, offspringPopulation, objective, searchSpace, problem);
}
