using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator<in TInput, in TContext, out TOutput>
{
  TOutput Apply(TInput input, TContext context);
}

public interface IOptimizationContext<TG, out TS, out TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  TS SearchSpace { get; }
  TP Problem { get; }
  IRandomNumberGenerator Random { get; }
}

public record OptimizationContext<TG, TS, TP>(
  TS SearchSpace,
  TP Problem,
  IRandomNumberGenerator Random
) : IOptimizationContext<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
}
