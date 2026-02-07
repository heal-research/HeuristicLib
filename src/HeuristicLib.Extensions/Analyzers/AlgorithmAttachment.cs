using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

public static class AlgorithmAttachmentExtensions {
  extension<TG, TS, TP, TR, TA, TBuildSpec>(IAlgorithmBuilder<TG, TS, TP, TR, TA, TBuildSpec> builder)
    where TA : IAlgorithm<TG, TS, TP, TR>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
    where TG : class
    where TBuildSpec : AlgorithmBuildSpec<TG, TS, TP, TR>
  {
    public void AddAttachment(AlgorithmAttachment<TG, TS, TP, TR> attachment) {
      builder.AddRewriter(attachment);
    }
  }
}

// public class ObserverRewriter<TG, TS, TP, TR, TSpec> : IAlgorithmBuilderRewriter<TSpec>
//   where TG : class
//   where TS : class, ISearchSpace<TG>
//   where TP : class, IProblem<TG, TS>
//   where TR : class, IAlgorithmState
//   where TSpec : AlgorithmBuildSpec<TG, TS, TP, TR>
// {
//   //private readonly IEvaluatorObserver<TG, TS, TP>? evaluatorObserver;
//   private readonly IIterationObserver<TG, TS, TP, TR>? iterationObserver;
//   private readonly ICrossoverObserver<TG, TS, TP>? crossoverObserver;
//
//   public ObserverRewriter(IIterationObserver<TG, TS, TP, TR>? iterationObserver = null, ICrossoverObserver<TG, TS, TP>? crossoverObserver = null)
//   {
//     this.iterationObserver = iterationObserver;
//     this.crossoverObserver = crossoverObserver;
//   }
//
//
//   public void Rewrite(TSpec buildSpec)
//   {
//     if (iterationObserver is not null) {
//       buildSpec.Observer = buildSpec.Observer is not null ? new ObservationPipeline<TG, TS, TP, TR>(buildSpec.Observer, iterationObserver) : iterationObserver;
//     }
//
//     if (crossoverObserver is not null && buildSpec is ISpecWithCrossover<TG, TS, TP> crossoverSpec) {
//       crossoverSpec.Crossover = new ObservedCrossover<TG, TS, TP>(crossoverSpec.Crossover, crossoverObserver);
//     }
//   }
// }

// ToDo: Think about a different name. Maybe UniversalRewriter or something similar. 
public abstract class AlgorithmAttachment<T, TS, TP, TRes> : IAlgorithmBuilderRewriter<AlgorithmBuildSpec<T, TS, TP, TRes>>
  where T : class
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TRes : class, IAlgorithmState 
{

  public void Rewrite(AlgorithmBuildSpec<T, TS, TP, TRes> buildSpec) {
    buildSpec.Evaluator = WrapEvaluator(buildSpec.Evaluator);
    buildSpec.Terminator = WrapTerminator(buildSpec.Terminator);
    buildSpec.Interceptor = WrapInterceptor(buildSpec.Interceptor);
    
    if (buildSpec is ISpecWithCreator<T, TS, TP> creatorSpec) creatorSpec.Creator = WrapCreator(creatorSpec.Creator);
    if (buildSpec is ISpecWithMutator<T, TS, TP> mutatorSpec) mutatorSpec.Mutator = WrapMutator(mutatorSpec.Mutator);
    if (buildSpec is ISpecWithCrossover<T, TS, TP> crossoverSpec) crossoverSpec.Crossover = WrapCrossover(crossoverSpec.Crossover);
    if (buildSpec is ISpecWithSelector<T, TS, TP> selectorSpec) selectorSpec.Selector = WrapSelector(selectorSpec.Selector);
  }
  
  public virtual IInterceptor<T1, TRes1, TS1, TP1>? WrapInterceptor<T1, TRes1, TS1, TP1>(IInterceptor<T1, TRes1, TS1, TP1>? interceptor) where T1 : class, T
    where TRes1 : class, IAlgorithmState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => interceptor;
  
  public virtual ITerminator<T1, TRes1, TS1, TP1> WrapTerminator<T1, TRes1, TS1, TP1>(ITerminator<T1, TRes1, TS1, TP1> terminator) where T1 : class, T
    where TRes1 : class, IAlgorithmState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => terminator;
  
  public virtual IEvaluator<T1, TS1, TP1> WrapEvaluator<T1, TS1, TP1>(IEvaluator<T1, TS1, TP1> evaluator) where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => evaluator;
  
  public virtual IMutator<T1, TS1, TP1> WrapMutator<T1, TS1, TP1>(IMutator<T1, TS1, TP1> mutator) where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => mutator;
  
  public virtual ICrossover<T1, TS1, TP1> WrapCrossover<T1, TS1, TP1>(ICrossover<T1, TS1, TP1> crossover) where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => crossover;
  
  public virtual ISelector<T1, TS1, TP1> WrapSelector<T1, TS1, TP1>(ISelector<T1, TS1, TP1> selector) where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => selector;
  
  public virtual ICreator<T1, TS1, TP1> WrapCreator<T1, TS1, TP1>(ICreator<T1, TS1, TP1> creator) where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => creator;
}

public abstract class AlgorithmAttachment<T> : AlgorithmAttachment<T, IAlgorithmState>
  where T : class;

public abstract class AlgorithmAttachment<T, TRes> : AlgorithmAttachment<T, ISearchSpace<T>, TRes>
  where T : class where TRes : class, IAlgorithmState;

public abstract class AlgorithmAttachment<T, TS, TRes> : AlgorithmAttachment<T, TS, IProblem<T, TS>, TRes>
  where T : class where TS : class, ISearchSpace<T> where TRes : class, IAlgorithmState;
