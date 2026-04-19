using Generator.Equals;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public partial record ObservableMutator<TG, TS, TP>
  : WrappingMutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  [OrderedEquality]
  public ImmutableArray<IMutatorObserver<TG, TS, TP>> Observers { get; }

  public ObservableMutator(IMutator<TG, TS, TP> mutator, ImmutableArray<IMutatorObserver<TG, TS, TP>> observers)
    : base(mutator)
  {
    Observers = observers;
  }

  public ObservableMutator(IMutator<TG, TS, TP> mutator, params IEnumerable<IMutatorObserver<TG, TS, TP>> observers)
    : this(mutator, [.. observers])
  {
  }

  protected override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parents, InnerMutate innerMutate, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var result = innerMutate(parents, random, searchSpace, problem);
    foreach (var observer in Observers) {
      observer.AfterMutate(result, parents, searchSpace, problem);
    }
    return result;
  }
}

public interface IMutatorObserver<in TG, in TS, in TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void AfterMutate(IReadOnlyList<TG> offspring, IReadOnlyList<TG> parent, TS searchSpace, TP problem);
}

public static class ObservableMutatorExtensions
{
  extension<TG, TS, TP>(IMutator<TG, TS, TP> mutator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IMutator<TG, TS, TP> ObserveWith(IMutatorObserver<TG, TS, TP> observer)
      => new ObservableMutator<TG, TS, TP>(mutator, observer);
    public IMutator<TG, TS, TP> ObserveWith(params IEnumerable<IMutatorObserver<TG, TS, TP>> observers)
      => new ObservableMutator<TG, TS, TP>(mutator, observers);
    public IMutator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>, IReadOnlyList<TG>, TS, TP> afterMutate)
      => mutator.ObserveWith(new ActionMutatorObserver<TG, TS, TP>(afterMutate));
    public IMutator<TG, TS, TP> ObserveWith(Action<IReadOnlyList<TG>> afterMutate)
      => mutator.ObserveWith(new ActionMutatorObserver<TG, TS, TP>((offspring, _, _, _) => afterMutate(offspring)));
    public IMutator<TG, TS, TP> CountInvocations(InvocationCounter counter)
      => mutator.ObserveWith(_ => counter.IncrementBy(1));
    public IMutator<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      return mutator.CountInvocations(counter);
    }
  }
}

public sealed class ActionMutatorObserver<TG, TS, TP>(Action<IReadOnlyList<TG>, IReadOnlyList<TG>, TS, TP> afterMutate) : IMutatorObserver<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public void AfterMutate(IReadOnlyList<TG> offspring, IReadOnlyList<TG> parent, TS searchSpace, TP problem) => afterMutate(offspring, parent, searchSpace, problem);
}
