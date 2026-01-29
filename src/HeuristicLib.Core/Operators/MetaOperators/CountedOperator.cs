using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MetaOperators;

public sealed class InvocationCounter
{
  private int currentCount = 0;

  public int CurrentCount => currentCount;
  
  public void Increment(int by = 1)
  {
    Interlocked.Add(ref currentCount, by);
  }
}

public static class CountedOperator
{
  // Maybe move to Mutator namespace?
  extension<TG, TS, TP>(IMutator<TG, TS, TP> mutator)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public CountedMutator<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      var countedMutator = new CountedMutator<TG, TS, TP>(mutator, counter);
      return countedMutator;
    }
    
    public CountedMutator<TG, TS, TP> CountInvocations(InvocationCounter counter)
    {
      var countedMutator = new CountedMutator<TG, TS, TP>(mutator, counter);
      return countedMutator;
    }
  }
  
  extension<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator)
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public CountedEvaluator<TG, TS, TP> CountInvocations(out InvocationCounter counter)
    {
      counter = new InvocationCounter();
      var countedEvaluator = new CountedEvaluator<TG, TS, TP>(evaluator, counter);
      return countedEvaluator;
    }
    
    public CountedEvaluator<TG, TS, TP> CountInvocations(InvocationCounter counter)
    {
      var countedEvaluator = new CountedEvaluator<TG, TS, TP>(evaluator, counter);
      return countedEvaluator;
    }
  }
}

// ToDo maybe move to Mutator namespace?
public class CountedMutator<TG, TS, TP> : Mutator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly IMutator<TG, TS, TP> mutator;
  private readonly InvocationCounter counter;
  
  public CountedMutator(IMutator<TG, TS, TP> mutator, InvocationCounter counter)
  {
    this.mutator = mutator;
    this.counter = counter;
  }

  public override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parent, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var results = mutator.Mutate(parent, random, searchSpace,problem);
    counter.Increment(by: results.Count);
    return results;
  }
}

public class CountedEvaluator<TG, TS, TP> : Evaluator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly IEvaluator<TG, TS, TP> evaluator;
  private readonly InvocationCounter counter;
  
  public CountedEvaluator(IEvaluator<TG, TS, TP> evaluator, InvocationCounter counter)
  {
    this.evaluator = evaluator;
    this.counter = counter;
  }

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TG> solutions, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var results = evaluator.Evaluate(solutions, random, searchSpace, problem);
    counter.Increment(by: solutions.Count);
    return results;
  }
}
 
