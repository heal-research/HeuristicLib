using System.Reflection;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public interface IAnalysis<T, TE, TP, in TProto, TRes>
  where TE : class, IEncoding<T>
  where TP : class, IProblem<T, TE>
  where TProto : IPrototype<T, TE, TP, TRes>
  where TRes : IIterationResult {
  public void AddToProto(TProto proto);
}

public abstract class Analysis<T, TE, TP, TProto, TRes> : IAnalysis<T, TE, TP, TProto, TRes>
  where TE : class, IEncoding<T>
  where TP : class, IProblem<T, TE>
  where TProto : IPrototype<T, TE, TP, TRes>
  where TRes : IIterationResult {
  public virtual void AddToProto(TProto proto) {
    if (proto is IMutatorPrototype<T, TE, TP> mutatorPrototype)
      mutatorPrototype.Mutator = WrapMutator(mutatorPrototype.Mutator);
    if (proto is ICrossoverPrototype<T, TE, TP> crossoverPrototype)
      crossoverPrototype.Crossover = WrapCrossover(crossoverPrototype.Crossover);
    if (proto is ISelectorPrototype<T, TE, TP> selectorPrototype)
      selectorPrototype.Selector = WrapSelector(selectorPrototype.Selector);
    proto.Evaluator = WrapEvaluator(proto.Evaluator);
    proto.Terminator = WrapTerminator(proto.Terminator);
    proto.Interceptor = WrapInterceptor(proto.Interceptor);
  }

  public virtual IInterceptor<T, TRes, TE, TP>? WrapInterceptor(IInterceptor<T, TRes, TE, TP>? interceptor) => interceptor;
  public virtual ITerminator<T, TRes, TE, TP> WrapTerminator(ITerminator<T, TRes, TE, TP> terminator) => terminator;
  public virtual IEvaluator<T, TE, TP> WrapEvaluator(IEvaluator<T, TE, TP> evaluator) => evaluator;
  public virtual IMutator<T, TE, TP> WrapMutator(IMutator<T, TE, TP> mutator) => mutator;
  public virtual ICrossover<T, TE, TP> WrapCrossover(ICrossover<T, TE, TP> crossover) => crossover;
  public virtual ISelector<T, TE, TP> WrapSelector(ISelector<T, TE, TP> selector) => selector;
}

public abstract class SimpleAnalysis<T, TE, TP, TRes>
  : Analysis<T, TE, TP, IPrototype<T, TE, TP, TRes>, TRes>
  where TE : class, IEncoding<T>
  where TP : class, IProblem<T, TE>
  where TRes : IIterationResult {
  #region Reflection Magic to avoid unnecessary wrapping
  // cache which hooks are actually overridden
  private readonly bool hasAfterEval;
  private readonly bool hasAfterIntercept;
  private readonly bool hasAfterTerminate;
  private readonly bool hasAfterMutate;
  private readonly bool hasAfterCross;
  private readonly bool hasAfterSelect;

  // ---- helper to detect overrides once ----
  private bool IsOverridden(string name, Type[] parameters) {
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    var m = GetType().GetMethod(name, flags, binder: null, types: parameters, modifiers: null);
    if (m == null)
      return false;

    // If the method is declared on SimpleAnalysis<,,,>, it wasn't overridden.
    var decl = m.DeclaringType;
    if (decl == null)
      return false;

    var isBase = decl.IsGenericType && decl.GetGenericTypeDefinition() == typeof(SimpleAnalysis<,,,>);
    return !isBase;
  }

  protected SimpleAnalysis() {
    hasAfterEval = IsOverridden(nameof(AfterEvaluation), [typeof(IReadOnlyList<T>), typeof(IReadOnlyList<ObjectiveVector>), typeof(TE), typeof(TP)]);
    hasAfterIntercept = IsOverridden(nameof(AfterInterception), [typeof(TRes), typeof(TRes), typeof(TE), typeof(TP)]);
    hasAfterTerminate = IsOverridden(nameof(AfterTermination), [typeof(bool), typeof(TRes), typeof(TRes), typeof(TE), typeof(TP)]);
    hasAfterMutate = IsOverridden(nameof(AfterMutation), [typeof(IReadOnlyList<T>), typeof(IReadOnlyList<T>), typeof(IRandomNumberGenerator), typeof(TE), typeof(TP)]);
    hasAfterCross = IsOverridden(nameof(AfterCrossover), [typeof(IReadOnlyList<T>), typeof(IReadOnlyList<ValueTuple<T, T>>), typeof(IRandomNumberGenerator), typeof(TE), typeof(TP)]);
    hasAfterSelect = IsOverridden(nameof(AfterSelection), [typeof(IReadOnlyList<Solution<T>>), typeof(IReadOnlyList<Solution<T>>), typeof(Objective), typeof(int), typeof(IRandomNumberGenerator), typeof(TE), typeof(TP)]);
  }

  // only wrap when needed
  public sealed override IInterceptor<T, TRes, TE, TP>? WrapInterceptor(IInterceptor<T, TRes, TE, TP>? interceptor)
    => hasAfterIntercept ? new AnalysisInterceptor(this, interceptor) : interceptor;

  public sealed override ITerminator<T, TRes, TE, TP> WrapTerminator(ITerminator<T, TRes, TE, TP> terminator)
    => hasAfterTerminate ? new AnalysisTerminator(this, terminator) : terminator;

  public sealed override IEvaluator<T, TE, TP> WrapEvaluator(IEvaluator<T, TE, TP> evaluator)
    => hasAfterEval ? new AnalysisEvaluator(this, evaluator) : evaluator;

  public sealed override IMutator<T, TE, TP> WrapMutator(IMutator<T, TE, TP> mutator)
    => hasAfterMutate ? new AnalysisMutator(this, mutator) : mutator;

  public sealed override ICrossover<T, TE, TP> WrapCrossover(ICrossover<T, TE, TP> crossover)
    => hasAfterCross ? new AnalysisCrossOver(this, crossover) : crossover;

  public sealed override ISelector<T, TE, TP> WrapSelector(ISelector<T, TE, TP> selector)
    => hasAfterSelect ? new AnalysisSelector(this, selector) : selector;
  #endregion

  #region wrappers (unchanged behavior)
  private sealed class AnalysisInterceptor(
    SimpleAnalysis<T, TE, TP, TRes> analysis,
    IInterceptor<T, TRes, TE, TP>? interceptor) : IInterceptor<T, TRes, TE, TP> {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TRes Transform(TRes currentIterationResult, TRes? previousIterationResult, TE encoding, TP problem) {
      var res = interceptor != null
        ? interceptor.Transform(currentIterationResult, previousIterationResult, encoding, problem)
        : currentIterationResult;
      analysis.AfterInterception(res, previousIterationResult, encoding, problem);
      return res;
    }
  }

  private sealed class AnalysisTerminator(
    SimpleAnalysis<T, TE, TP, TRes> analysis,
    ITerminator<T, TRes, TE, TP> terminator) : ITerminator<T, TRes, TE, TP> {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldTerminate(TRes currentIterationState, TRes? previousIterationState, TE encoding, TP problem) {
      var res = terminator.ShouldTerminate(currentIterationState, previousIterationState, encoding, problem);
      analysis.AfterTermination(res, currentIterationState, previousIterationState, encoding, problem);
      return res;
    }
  }

  private sealed class AnalysisEvaluator(
    SimpleAnalysis<T, TE, TP, TRes> analysis,
    IEvaluator<T, TE, TP> evaluator) : IEvaluator<T, TE, TP> {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<T> genotypes, IRandomNumberGenerator random, TE encoding, TP problem) {
      var q = evaluator.Evaluate(genotypes, random, encoding, problem);
      analysis.AfterEvaluation(genotypes, q, encoding, problem);
      return q;
    }
  }

  private sealed class AnalysisMutator(SimpleAnalysis<T, TE, TP, TRes> analysis, IMutator<T, TE, TP> mutator) : IMutator<T, TE, TP> {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<T> Mutate(IReadOnlyList<T> parent, IRandomNumberGenerator random, TE encoding, TP problem) {
      var res = mutator.Mutate(parent, random, encoding, problem);
      analysis.AfterMutation(res, parent, random, encoding, problem);
      return res;
    }
  }

  private sealed class AnalysisCrossOver(SimpleAnalysis<T, TE, TP, TRes> analysis, ICrossover<T, TE, TP> crossover)
    : ICrossover<T, TE, TP> {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<T> Cross(IReadOnlyList<(T, T)> parents, IRandomNumberGenerator random, TE encoding, TP problem) {
      var res = crossover.Cross(parents, random, encoding, problem);
      analysis.AfterCrossover(res, parents, random, encoding, problem);
      return res;
    }
  }

  private sealed class AnalysisSelector(SimpleAnalysis<T, TE, TP, TRes> analysis, ISelector<T, TE, TP> selector)
    : ISelector<T, TE, TP> {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<Solution<T>> Select(IReadOnlyList<Solution<T>> population, Objective objective, int count, IRandomNumberGenerator random, TE encoding, TP problem) {
      var res = selector.Select(population, objective, count, random, encoding, problem);
      analysis.AfterSelection(res, population, objective, count, random, encoding, problem);
      return res;
    }
  }
  #endregion

  // ---- default hooks (optionally overridden by users) ----
  protected virtual void AfterEvaluation(IReadOnlyList<T> genotypes, IReadOnlyList<ObjectiveVector> values, TE encoding, TP problem) { }
  protected virtual void AfterInterception(TRes currentIterationResult, TRes? previousIterationResult, TE encoding, TP problem) { }
  protected virtual void AfterTermination(bool res, TRes currentIterationState, TRes? previousIterationState, TE encoding, TP problem) { }
  protected virtual void AfterMutation(IReadOnlyList<T> res, IReadOnlyList<T> parent, IRandomNumberGenerator random, TE encoding, TP problem) { }
  protected virtual void AfterCrossover(IReadOnlyList<T> res, IReadOnlyList<(T, T)> parents, IRandomNumberGenerator random, TE encoding, TP problem) { }
  protected virtual void AfterSelection(IReadOnlyList<Solution<T>> res, IReadOnlyList<Solution<T>> population, Objective objective, int count, IRandomNumberGenerator random, TE encoding, TP problem) { }
}
