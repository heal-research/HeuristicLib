using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Observation;

public sealed class ObservedCrossover<TG, TS, TP> : Crossover<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly ICrossover<TG, TS, TP> crossover;
  private readonly ICrossoverObserver<TG, TS, TP> observer;

  public ObservedCrossover(ICrossover<TG, TS, TP> crossover, ICrossoverObserver<TG, TS, TP> observer)
  {
    this.crossover = crossover;
    this.observer = observer;
  }

  public override IReadOnlyList<TG> Cross(IReadOnlyList<IParents<TG>> parents, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var offspring = crossover.Cross(parents, random, searchSpace, problem);
    observer.OnCrossoverCompleted(parents, offspring, searchSpace, problem);
    return offspring;
  }
}

public static class CrossoverObserver
{
  public static ICrossoverObserver<TG, TS, TP> Create<TG, TS, TP>(Action<IReadOnlyList<IParents<TG>>, IReadOnlyList<TG>, TS, TP> onCrossoverCompleted)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS> =>
    new FuncCrossoverObserver<TG, TS, TP>(onCrossoverCompleted);
}

public interface ICrossoverObserver<in TG, in TS, in TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  void OnCrossoverCompleted(IReadOnlyList<IParents<TG>> parents, IReadOnlyList<TG> offspring, TS searchSpace, TP problem);
}

public abstract class CrossoverObserver<TG, TS, TP> : ICrossoverObserver<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public abstract void OnCrossoverCompleted(IReadOnlyList<IParents<TG>> parents, IReadOnlyList<TG> offspring, TS searchSpace, TP problem);
}

public abstract class CrossoverObserver<TG, TS> : ICrossoverObserver<TG, TS, IProblem<TG, TS>>
  where TG : class
  where TS : class, ISearchSpace<TG>
{
  public abstract void OnCrossoverCompleted(IReadOnlyList<IParents<TG>> parents, IReadOnlyList<TG> offspring, TS searchSpace);
  public void OnCrossoverCompleted(IReadOnlyList<IParents<TG>> parents, IReadOnlyList<TG> offspring, TS searchSpace, IProblem<TG, TS> problem) => OnCrossoverCompleted(parents, offspring, searchSpace);
}

public abstract class CrossoverObserver<TG> : ICrossoverObserver<TG, ISearchSpace<TG>, IProblem<TG, ISearchSpace<TG>>>
  where TG : class
{
  public abstract void OnCrossoverCompleted(IReadOnlyList<IParents<TG>> parents, IReadOnlyList<TG> offspring);
  public void OnCrossoverCompleted(IReadOnlyList<IParents<TG>> parents, IReadOnlyList<TG> offspring, ISearchSpace<TG> searchSpace, IProblem<TG, ISearchSpace<TG>> problem) => OnCrossoverCompleted(parents, offspring);
}

public sealed class FuncCrossoverObserver<TG, TS, TP>(Action<IReadOnlyList<IParents<TG>>, IReadOnlyList<TG>, TS, TP> onCrossoverCompleted) : CrossoverObserver<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public override void OnCrossoverCompleted(IReadOnlyList<IParents<TG>> parents, IReadOnlyList<TG> offspring, TS searchSpace, TP problem) => onCrossoverCompleted(parents, offspring, searchSpace, problem);
}
