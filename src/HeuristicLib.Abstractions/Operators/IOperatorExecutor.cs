using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators;

public interface IOperatorExecutor
{
  TResult Execute<TOperatorInstance, TResult>(
    IOperator<TOperatorInstance> op,
    Func<TOperatorInstance, TResult> execute)
    where TOperatorInstance : class, IOperatorInstance;
}

public static class OperatorExecutorExtensions
{
  extension(IOperatorExecutor executor)
  {
    public IReadOnlyList<TGenotype> Create<TGenotype, TSearchSpace, TProblem>(
      ICreator<TGenotype, TSearchSpace, TProblem> creator,
      int count,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem)
      where TSearchSpace : class, ISearchSpace<TGenotype>
      where TProblem : class, IProblem<TGenotype, TSearchSpace>
    {
      return executor.Execute(creator, instance => instance.Create(count, random, searchSpace, problem));
    }

    public IReadOnlyList<TGenotype> Mutate<TGenotype, TSearchSpace, TProblem>(
      IMutator<TGenotype, TSearchSpace, TProblem> mutator,
      IReadOnlyList<TGenotype> parents,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem)
      where TSearchSpace : class, ISearchSpace<TGenotype>
      where TProblem : class, IProblem<TGenotype, TSearchSpace>
    {
      return executor.Execute(mutator, instance => instance.Mutate(parents, random, searchSpace, problem));
    }

    public IReadOnlyList<TGenotype> Cross<TGenotype, TSearchSpace, TProblem>(
      ICrossover<TGenotype, TSearchSpace, TProblem> crossover,
      IReadOnlyList<IParents<TGenotype>> parents,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem)
      where TSearchSpace : class, ISearchSpace<TGenotype>
      where TProblem : class, IProblem<TGenotype, TSearchSpace>
    {
      return executor.Execute(crossover, instance => instance.Cross(parents, random, searchSpace, problem));
    }

    public IReadOnlyList<ObjectiveVector> Evaluate<TGenotype, TSearchSpace, TProblem>(
      IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,
      IReadOnlyList<TGenotype> genotypes,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem)
      where TSearchSpace : class, ISearchSpace<TGenotype>
      where TProblem : class, IProblem<TGenotype, TSearchSpace>
    {
      return executor.Execute(evaluator, instance => instance.Evaluate(genotypes, random, searchSpace, problem));
    }

    public IReadOnlyList<ISolution<TGenotype>> Select<TGenotype, TSearchSpace, TProblem>(
      ISelector<TGenotype, TSearchSpace, TProblem> selector,
      IReadOnlyList<ISolution<TGenotype>> population,
      Objective objective,
      int count,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem)
      where TSearchSpace : class, ISearchSpace<TGenotype>
      where TProblem : class, IProblem<TGenotype, TSearchSpace>
    {
      return executor.Execute(selector, instance => instance.Select(population, objective, count, random, searchSpace, problem));
    }

    public IReadOnlyList<ISolution<TGenotype>> Replace<TGenotype, TSearchSpace, TProblem>(
      IReplacer<TGenotype, TSearchSpace, TProblem> replacer,
      IReadOnlyList<ISolution<TGenotype>> previousPopulation,
      IReadOnlyList<ISolution<TGenotype>> offspringPopulation,
      Objective objective,
      int count,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem)
      where TSearchSpace : class, ISearchSpace<TGenotype>
      where TProblem : class, IProblem<TGenotype, TSearchSpace>
    {
      return executor.Execute(replacer, instance => instance.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem));
    }

    public TAlgorithmState Transform<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(
      IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState> interceptor,
      TAlgorithmState currentState,
      TAlgorithmState? previousState,
      TSearchSpace searchSpace,
      TProblem problem)
      where TAlgorithmState : class, IAlgorithmState
      where TSearchSpace : class, ISearchSpace<TGenotype>
      where TProblem : class, IProblem<TGenotype, TSearchSpace>
    {
      return executor.Execute(interceptor, instance => instance.Transform(currentState, previousState, searchSpace, problem));
    }

    public bool ShouldTerminate<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(
      ITerminator<TGenotype, TSearchSpace, TProblem, TAlgorithmState> terminator,
      TAlgorithmState state,
      TSearchSpace searchSpace,
      TProblem problem)
      where TAlgorithmState : IAlgorithmState
      where TSearchSpace : class, ISearchSpace<TGenotype>
      where TProblem : IProblem<TGenotype, TSearchSpace>
    {
      return executor.Execute(terminator, instance => instance.ShouldTerminate(state, searchSpace, problem));
    }
  }
}
