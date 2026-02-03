using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract class Mutator<TGenotype, TSearchSpace, TProblem> : IMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  IReadOnlyList<TGenotype> IOperator<IReadOnlyList<TGenotype>, IOptimizationContext<TGenotype, TSearchSpace, TProblem>, IReadOnlyList<TGenotype>>.Execute(IReadOnlyList<TGenotype> input, IOptimizationContext<TGenotype, TSearchSpace, TProblem> context)
  {
    return Mutate(input, context.Random, context.SearchSpace, context.Problem);
  }
}

public abstract class Mutator<TGenotype, TSearchSpace> : IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace);

  IReadOnlyList<TGenotype> IMutator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Mutate(parent, random, searchSpace);

  IReadOnlyList<TGenotype> IOperator<IReadOnlyList<TGenotype>, IOptimizationContext<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>, IReadOnlyList<TGenotype>>.Execute(IReadOnlyList<TGenotype> input, IOptimizationContext<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> context) =>
    Mutate(input, context.Random, context.SearchSpace);
}

public abstract class Mutator<TGenotype> : IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random);

  IReadOnlyList<TGenotype> IMutator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Mutate(IReadOnlyList<TGenotype> parent, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Mutate(parent, random);

  IReadOnlyList<TGenotype> IOperator<IReadOnlyList<TGenotype>, IOptimizationContext<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>, IReadOnlyList<TGenotype>>.Execute(IReadOnlyList<TGenotype> input, IOptimizationContext<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> context) => 
    Mutate(input, context.Random);
}

public class MutatorAdapter<TG, TS, TP> : Mutator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly IOperator<IReadOnlyList<TG>, IOptimizationContext<TG, TS, TP>, IReadOnlyList<TG>> @operator;

  public MutatorAdapter(IOperator<IReadOnlyList<TG>, IOptimizationContext<TG, TS, TP>, IReadOnlyList<TG>> @operator)
  {
    this.@operator = @operator;
  }

  public override IReadOnlyList<TG> Mutate(IReadOnlyList<TG> parent, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var context = new OptimizationContext<TG, TS, TP>(searchSpace, problem, random);
    return @operator.Execute(parent, context);
  }
}

public static class MutatorAdapter
{
  extension<TG, TS, TP>(IOperator<IReadOnlyList<TG>, IOptimizationContext<TG, TS, TP>, IReadOnlyList<TG>> @operator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IMutator<TG, TS, TP> AsMutator()
    {
      return new MutatorAdapter<TG, TS, TP>(@operator);
    }
  }
  
  extension<TG, TS, TP>(IMutator<TG, TS, TP> mutator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IOperator<IReadOnlyList<TG>, IOptimizationContext<TG, TS, TP>, IReadOnlyList<TG>> AsOperator()
    {
      return mutator;
    }
  }
}
