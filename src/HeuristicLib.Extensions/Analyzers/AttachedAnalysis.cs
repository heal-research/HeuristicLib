using System.Reflection;
using System.Runtime.CompilerServices;
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

namespace HEAL.HeuristicLib.Analyzers;

public abstract class AttachedAnalysis<T, TS, TP, TRes>
  : AlgorithmAttachment<T, TS, TP, TRes>
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TRes : class, IAlgorithmState
  where T : class {
  #region Reflection Magic to avoid unnecessary wrapping
  // cache which hooks are actually overridden
  private readonly bool hasAfterEval;
  private readonly bool hasAfterIntercept;
  private readonly bool hasAfterTerminate;
  private readonly bool hasAfterMutate;
  private readonly bool hasAfterCross;
  private readonly bool hasAfterSelect;
  private readonly bool hasAfterCreate;

  // ---- helper to detect overrides once ----
  private bool IsOverridden(string name, Type[] parameters) {
#pragma warning disable S3011
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
#pragma warning restore S3011
    var m = GetType().GetMethod(name, flags, binder: null, types: parameters, modifiers: null);
    if (m == null)
      return false;

    // If the method is declared on AttachedAnalysis<,,,>, it wasn't overridden.
    var decl = m.DeclaringType;
    if (decl == null)
      return false;

    var isBase = decl.IsGenericType && decl.GetGenericTypeDefinition() == typeof(AttachedAnalysis<,,,>);
    return !isBase;
  }

  protected AttachedAnalysis() {
    hasAfterEval = IsOverridden(nameof(AfterEvaluation), [typeof(IReadOnlyList<T>), typeof(IReadOnlyList<ObjectiveVector>), typeof(TS), typeof(TP)]);
    hasAfterIntercept = IsOverridden(nameof(AfterInterception), [typeof(TRes), typeof(TRes), typeof(TS), typeof(TP)]);
    hasAfterTerminate = IsOverridden(nameof(AfterTermination), [typeof(bool), typeof(TRes), typeof(TRes), typeof(TS), typeof(TP)]);
    hasAfterMutate = IsOverridden(nameof(AfterMutation), [typeof(IReadOnlyList<T>), typeof(IReadOnlyList<T>), typeof(IRandomNumberGenerator), typeof(TS), typeof(TP)]);
    hasAfterCross = IsOverridden(nameof(AfterCrossover), [typeof(IReadOnlyList<T>), typeof(IReadOnlyList<IParents<T>>), typeof(IRandomNumberGenerator), typeof(TS), typeof(TP)]);
    hasAfterSelect = IsOverridden(nameof(AfterSelection), [typeof(IReadOnlyList<ISolution<T>>), typeof(IReadOnlyList<ISolution<T>>), typeof(Objective), typeof(int), typeof(IRandomNumberGenerator), typeof(TS), typeof(TP)]);
    hasAfterCreate = IsOverridden(nameof(AfterCreation), [typeof(IReadOnlyList<T>), typeof(int), typeof(IRandomNumberGenerator), typeof(TS), typeof(TP)]);
  }
  //IReadOnlyList<T> res, int count, IRandomNumberGenerator random, TS encoding, TP problem

  public override IInterceptor<T1, TRes1, TS1, TP1>?
    WrapInterceptor<T1, TRes1, TS1, TP1>(IInterceptor<T1, TRes1, TS1, TP1>? interceptor) => hasAfterIntercept
    ? new AnalysisInterceptor<T1, TRes1, TS1, TP1>(this, interceptor)
    : interceptor;

  public override ITerminator<T1, TRes1, TS1, TP1>
    WrapTerminator<T1, TRes1, TS1, TP1>(ITerminator<T1, TRes1, TS1, TP1> terminator) => hasAfterTerminate
    ? new AnalysisTerminator<T1, TRes1, TS1, TP1>(this, terminator)
    : terminator;

  public override IEvaluator<T1, TS1, TP1> WrapEvaluator<T1, TS1, TP1>(IEvaluator<T1, TS1, TP1> evaluator) =>
    hasAfterEval ? new AnalysisEvaluator<T1, TS1, TP1>(this, evaluator) : evaluator;

  public override ICrossover<T1, TS1, TP1> WrapCrossover<T1, TS1, TP1>(ICrossover<T1, TS1, TP1> crossover) =>
    hasAfterCross ? new AnalysisCrossOver<T1, TS1, TP1>(this, crossover) : crossover;

  public override IMutator<T1, TS1, TP1> WrapMutator<T1, TS1, TP1>(IMutator<T1, TS1, TP1> mutator) =>
    hasAfterMutate ? new AnalysisMutator<T1, TS1, TP1>(this, mutator) : mutator;

  public override ISelector<T1, TS1, TP1> WrapSelector<T1, TS1, TP1>(ISelector<T1, TS1, TP1> selector) =>
    hasAfterSelect ? new AnalysisSelector<T1, TS1, TP1>(this, selector) : selector;

  public override ICreator<T1, TS1, TP1> WrapCreator<T1, TS1, TP1>(ICreator<T1, TS1, TP1> creator) =>
    hasAfterCreate ? new AnalysisCreator<T1, TS1, TP1>(this, creator) : creator;
  #endregion

  #region wrappers (unchanged behavior)
  private sealed class AnalysisInterceptor<T1, TRes1, TS1, TP1>(
    AttachedAnalysis<T, TS, TP, TRes> analysis,
    IInterceptor<T1, TRes1, TS1, TP1>? interceptor) : IInterceptor<T1, TRes1, TS1, TP1>
    where TRes1 : class, IAlgorithmState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TRes1 Transform(TRes1 currentState, TRes1? previousState, TS1 searchSpace, TP1 problem) {
      var res = interceptor != null
        ? interceptor.Transform(currentState, previousState, searchSpace, problem)
        : currentState;
      analysis.AfterInterception(res, previousState, searchSpace, problem);
      return res;
    }
  }

  private sealed class AnalysisTerminator<T1, TRes1, TS1, TP1>(
    AttachedAnalysis<T, TS, TP, TRes> analysis,
    ITerminator<T1, TRes1, TS1, TP1> terminator) : ITerminator<T1, TRes1, TS1, TP1>
    where TRes1 : IAlgorithmState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP {
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public bool ShouldTerminate(TRes1 currentIterationState, TRes1? previousIterationState, TS1 searchSpace, TP1 problem) {
    //   var res = terminator.ShouldTerminate(currentIterationState, previousIterationState, searchSpace, problem);
    //   analysis.AfterTermination(res, currentIterationState, previousIterationState, searchSpace, problem);
    //   return res;
    // }
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

  private sealed class AnalysisEvaluator<T1, TS1, TP1>(
    AttachedAnalysis<T, TS, TP, TRes> analysis,
    IEvaluator<T1, TS1, TP1> evaluator) : IEvaluator<T1, TS1, TP1>
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<T1> genotypes, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem) {
      var q = evaluator.Evaluate(genotypes, random, searchSpace, problem);
      analysis.AfterEvaluation(genotypes, q, searchSpace, problem);
      return q;
    }
    public IReadOnlyList<ObjectiveVector> Execute(IReadOnlyList<T1> input, IOptimizationContext<T1, TS1, TP1> context) => Evaluate(input, context.Random, context.SearchSpace, context.Problem);
  }

  private sealed class AnalysisMutator<T1, TS1, TP1>(
    AttachedAnalysis<T, TS, TP, TRes> analysis,
    IMutator<T1, TS1, TP1> mutator) : IMutator<T1, TS1, TP1>
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<T1> Mutate(IReadOnlyList<T1> parent, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem) {
      var res = mutator.Mutate(parent, random, searchSpace, problem);
      analysis.AfterMutation(res, parent, random, searchSpace, problem);
      return res;
    }
    public IReadOnlyList<T1> Execute(IReadOnlyList<T1> input, IOptimizationContext<T1, TS1, TP1> context) => Mutate(input, context.Random, context.SearchSpace, context.Problem);
  }

  private sealed class AnalysisCrossOver<T1, TS1, TP1>(AttachedAnalysis<T, TS, TP, TRes> analysis, ICrossover<T1, TS1, TP1> crossover)
    : ICrossover<T1, TS1, TP1>
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<T1> Cross(IReadOnlyList<IParents<T1>> parents, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem) {
      var res = crossover.Cross(parents, random, searchSpace, problem);
      analysis.AfterCrossover(res, parents, random, searchSpace, problem);
      return res;
    }
  }

  private sealed class AnalysisSelector<T1, TS1, TP1>(AttachedAnalysis<T, TS, TP, TRes> analysis, ISelector<T1, TS1, TP1> selector)
    : ISelector<T1, TS1, TP1>
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<ISolution<T1>> Select(IReadOnlyList<ISolution<T1>> population, Objective objective, int count, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem) {
      var res = selector.Select(population, objective, count, random, searchSpace, problem);
      analysis.AfterSelection(res, population, objective, count, random, searchSpace, problem);
      return res;
    }
  }

  private sealed class AnalysisCreator<T1, TS1, TP1>(AttachedAnalysis<T, TS, TP, TRes> analysis, ICreator<T1, TS1, TP1> selector)
    : ICreator<T1, TS1, TP1>
    where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<T1> Create(int count, IRandomNumberGenerator random, TS1 searchSpace, TP1 problem) {
      var res = selector.Create(count, random, searchSpace, problem);
      analysis.AfterCreation(res, count, random, searchSpace, problem);
      return res;
    }
  }
  #endregion

  // ---- default hooks (optionally overridden by users) ----
  public virtual void AfterEvaluation(IReadOnlyList<T> genotypes, IReadOnlyList<ObjectiveVector> values, TS searchSpace, TP problem) { }
  public virtual void AfterInterception(TRes currentIterationState, TRes? previousIterationState, TS searchSpace, TP problem) { }
  public virtual void AfterTermination(bool res, TRes currentIterationState, TRes? previousIterationState, TS searchSpace, TP problem) { }
  public virtual void AfterMutation(IReadOnlyList<T> res, IReadOnlyList<T> parent, IRandomNumberGenerator random, TS searchSpace, TP problem) { }
  public virtual void AfterCrossover(IReadOnlyList<T> res, IReadOnlyList<IParents<T>> parents, IRandomNumberGenerator random, TS searchSpace, TP problem) { }
  public virtual void AfterSelection(IReadOnlyList<ISolution<T>> res, IReadOnlyList<ISolution<T>> population, Objective objective, int count, IRandomNumberGenerator random, TS searchSpace, TP problem) { }
  public virtual void AfterCreation(IReadOnlyList<T> res, int count, IRandomNumberGenerator random, TS searchSpace, TP problem) { }
}


public abstract class AttachedAnalysis<T> : AttachedAnalysis<T, IAlgorithmState>
  where T : class;

public abstract class AttachedAnalysis<T, TRes> : AttachedAnalysis<T, ISearchSpace<T>, TRes>
  where TRes : class, IAlgorithmState
  where T : class;

public abstract class AttachedAnalysis<T, TS, TRes> : AttachedAnalysis<T, TS, IProblem<T, TS>, TRes>
  where TS : class, ISearchSpace<T> where T : class where TRes : class, IAlgorithmState;
