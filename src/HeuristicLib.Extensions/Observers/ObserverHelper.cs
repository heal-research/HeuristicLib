using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Observers;

#region Interfaces
public interface IInterceptorObserver<in T, in TS, in TP, in TRes>
  where T : class
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TRes : class
{
  void AfterInterception(TRes res, TRes? previousState, TS searchSpace, TP problem);
}

public interface IMutatorObserver<in T, in TS, in TP>
  where T : class
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
{
  void AfterMutation(IReadOnlyList<T> res, IReadOnlyList<T> parent, IRandomNumberGenerator random, TS searchSpace, TP problem);
}

public interface ICrossoverObserver<in T, in TS, in TP>
  where T : class
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
{
  void AfterCrossover(IReadOnlyList<T> res, IReadOnlyList<IParents<T>> parents, IRandomNumberGenerator random, TS searchSpace, TP problem);
}

public interface ITerminatorObserver<in T, in TS, in TP, in TRes>
  where T : class
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TRes : class
{
  void AfterTermination(bool res, TRes currentIterationState, TRes? previousIterationState, TS searchSpace, TP problem);
}

public interface IEvaluatorObserver<in T, in TS, in TP>
  where T : class
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
{
  void AfterEvaluation(IReadOnlyList<T> genotypes, IReadOnlyList<ObjectiveVector> values, TS searchSpace, TP problem);
}

public interface ISelectorObserver<in T, in TS, in TP>
  where T : class
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
{
  void AfterSelection(IReadOnlyList<ISolution<T>> res, IReadOnlyList<ISolution<T>> population, Objective objective, int count, IRandomNumberGenerator random, TS searchSpace, TP problem);
}

public interface ICreatorObserver<in T, in TS, in TP>
  where T : class
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
{
  void AfterCreation(IReadOnlyList<T> res, int count, IRandomNumberGenerator random, TS searchSpace, TP problem) { }
}
#endregion

#region convenience Interfaces
public interface IInterceptorObserver<TGenotype> :
  IInterceptorObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, IAlgorithmState>
  where TGenotype : class
{ }

public interface IInterceptorObserver<TGenotype, in TState> :
  IInterceptorObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TState>
  where TGenotype : class
  where TState : class
{ }

public interface IEvaluatorObserver<TGenotype> :
  IEvaluatorObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TGenotype : class
{ }

public interface ICrossoverObserver<TGenotype> :
  ICrossoverObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TGenotype : class
{ }

public interface IMutatorObserver<TGenotype> :
  IMutatorObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TGenotype : class
{ }
#endregion

public static class ObserverHelper
{
  #region Wrapping Functions
  public static IInterceptor<T1, TRes1, TS1, TP1> WrapInterceptor<T1, TRes1, TS1, TP1, T, TS, TP, TRes>(this IInterceptorObserver<T, TS, TP, TRes> observer, IInterceptor<T1, TRes1, TS1, TP1> interceptor)
    where T1 : class, T
    where TRes1 : class, IAlgorithmState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
    where TRes : class, IAlgorithmState
    => new ObserverInterceptor<T1, TRes1, TS1, TP1, T, TS, TP, TRes>(observer, interceptor);

  public static ITerminator<T1, TRes1, TS1, TP1> WrapTerminator<T1, TRes1, TS1, TP1, T, TS, TP, TRes>(this ITerminatorObserver<T, TS, TP, TRes> observer, ITerminator<T1, TRes1, TS1, TP1> interceptor)
    where T1 : class, T
    where TRes1 : class, IAlgorithmState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
    where TRes : class, IAlgorithmState
    => new ObserverTerminator<T1, TRes1, TS1, TP1, T, TS, TP, TRes>(observer, interceptor);

  public static IEvaluator<T1, TS1, TP1> WrapEvaluator<T1, TS1, TP1, T, TS, TP>(this IEvaluatorObserver<T, TS, TP> observer, IEvaluator<T1, TS1, TP1> interceptor)
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS> => new ObserverEvaluator<T1, TS1, TP1, T, TS, TP>(observer, interceptor);

  public static IMutator<T1, TS1, TP1> WrapMutator<T1, TS1, TP1, T, TS, TP>(this IMutatorObserver<T, TS, TP> observer, IMutator<T1, TS1, TP1> interceptor)
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS> => new ObserverMutator<T1, TS1, TP1, T, TS, TP>(observer, interceptor);

  public static ICrossover<T1, TS1, TP1> WrapCrossover<T1, TS1, TP1, T, TS, TP>(this ICrossoverObserver<T, TS, TP> observer, ICrossover<T1, TS1, TP1> interceptor)
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS> => new ObserverCrossover<T1, TS1, TP1, T, TS, TP>(observer, interceptor);

  public static ISelector<T1, TS1, TP1> WrapSelector<T1, TS1, TP1, T, TS, TP>(this ISelectorObserver<T, TS, TP> observer, ISelector<T1, TS1, TP1> interceptor)
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS> => new ObserverSelector<T1, TS1, TP1, T, TS, TP>(observer, interceptor);

  public static ICreator<T1, TS1, TP1> WrapCreator<T1, TS1, TP1, T, TS, TP>(this ICreatorObserver<T, TS, TP> observer, ICreator<T1, TS1, TP1> interceptor)
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS> => new ObserverCreator<T1, TS1, TP1, T, TS, TP>(observer, interceptor);
  #endregion

  #region WrapperClasses
  private sealed class ObserverInterceptor<T1, TRes1, TS1, TP1, T, TS, TP, TRes>(
    IInterceptorObserver<T, TS, TP, TRes> observer,
    IInterceptor<T1, TRes1, TS1, TP1>? interceptor) : IInterceptor<T1, TRes1, TS1, TP1>
    where T1 : class, T
    where TRes1 : class, IAlgorithmState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
    where TRes : class, IAlgorithmState
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TRes1 Transform(TRes1 currentState, TRes1? previousState, TS1 searchSpace, TP1 problem)
    {
      var res = interceptor != null
        ? interceptor.Transform(currentState, previousState, searchSpace, problem)
        : currentState;
      observer.AfterInterception(res, previousState, searchSpace, problem);
      return res;
    }
  }

  private sealed class ObserverTerminator<T1, TRes1, TS1, TP1, T, TS, TP, TRes>(
    ITerminatorObserver<T, TS, TP, TRes> analysis,
    ITerminator<T1, TRes1, TS1, TP1> terminator) : ITerminator<T1, TRes1, TS1, TP1>
    where T1 : class, T
    where TRes1 : IAlgorithmState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
    where TRes : class, IAlgorithmState
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Func<TRes1, bool> CreateShouldTerminatePredicate(TS1 searchSpace, TP1 problem)
    {
      var terminatePredicate = terminator.CreateShouldContinuePredicate(searchSpace, problem);
      return currentIterationState => {
        var res = terminatePredicate(currentIterationState);
        analysis.AfterTermination(res, currentIterationState, null, searchSpace, problem);
        return res;
      };
    }
  }

  private sealed class ObserverEvaluator<T1, TS1, TP1, T, TS, TP>(
    IEvaluatorObserver<T, TS, TP> analysis,
    IEvaluator<T1, TS1, TP1> evaluator) : IEvaluator<T1, TS1, TP1>
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<T1> genotypes, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem)
    {
      var q = evaluator.Evaluate(genotypes, random, searchSpace, problem);
      analysis.AfterEvaluation(genotypes, q, searchSpace, problem);
      return q;
    }

    public IReadOnlyList<ObjectiveVector> Execute(IReadOnlyList<T1> input, IOptimizationContext<T1, TS1, TP1> context) => Evaluate(input, context.Random, context.SearchSpace, context.Problem);
  }

  private sealed class ObserverMutator<T1, TS1, TP1, T, TS, TP>(
    IMutatorObserver<T, TS, TP> analysis,
    IMutator<T1, TS1, TP1> mutator) : IMutator<T1, TS1, TP1>
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<T1> Mutate(IReadOnlyList<T1> parent, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem)
    {
      var res = mutator.Mutate(parent, random, searchSpace, problem);
      analysis.AfterMutation(res, parent, random, searchSpace, problem);
      return res;
    }

    public IReadOnlyList<T1> Execute(IReadOnlyList<T1> input, IOptimizationContext<T1, TS1, TP1> context) => Mutate(input, context.Random, context.SearchSpace, context.Problem);
  }

  private sealed class ObserverCrossover<T1, TS1, TP1, T, TS, TP>(
    ICrossoverObserver<T, TS, TP> analysis,
    ICrossover<T1, TS1, TP1> crossover) : ICrossover<T1, TS1, TP1>
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<T1> Cross(IReadOnlyList<IParents<T1>> parents, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem)
    {
      var res = crossover.Cross(parents, random, searchSpace, problem);
      analysis.AfterCrossover(res, parents, random, searchSpace, problem);
      return res;
    }
  }

  private sealed class ObserverSelector<T1, TS1, TP1, T, TS, TP>(ISelectorObserver<T, TS, TP> analysis, ISelector<T1, TS1, TP1> selector)
    : ISelector<T1, TS1, TP1>
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<ISolution<T1>> Select(IReadOnlyList<ISolution<T1>> population, Objective objective, int count, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem)
    {
      var res = selector.Select(population, objective, count, random, searchSpace, problem);
      analysis.AfterSelection(res, population, objective, count, random, searchSpace, problem);
      return res;
    }
  }

  private sealed class ObserverCreator<T1, TS1, TP1, T, TS, TP>(
    ICreatorObserver<T, TS, TP> analysis,
    ICreator<T1, TS1, TP1> selector)
    : ICreator<T1, TS1, TP1>
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP
    where T : class
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<T1> Create(int count, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem)
    {
      var res = selector.Create(count, random, searchSpace, problem);
      analysis.AfterCreation(res, count, random, searchSpace, problem);
      return res;
    }
  }
  #endregion
}
