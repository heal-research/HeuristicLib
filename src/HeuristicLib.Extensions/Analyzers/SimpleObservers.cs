using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

// shortening interfaces for simple cases where the search space and problem are not relevant for the analysis

public interface IInterceptorObserver<TGenotype> :
  IInterceptorObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, IAlgorithmState>;

public interface IInterceptorObserver<TGenotype, in TState> :
  IInterceptorObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, TState>
  where TState : class, IAlgorithmState;

public interface IEvaluatorObserver<TGenotype> :
  IEvaluatorObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>;

public interface ICrossoverObserver<TGenotype> :
  ICrossoverObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>;

public interface IMutatorObserver<TGenotype> :
  IMutatorObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>;

public static class BuilderExtensions
{
  public static void AttachObserver<TG, TR, TS, TP>(this IAlgorithmBuilder<TG, TS, TP, TR> builder, IExecutable<IExecutionInstance> analysis)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    var hasAttached = false;

    if (analysis is IEvaluatorObserver<TG, TS, TP> evaluatorObserver && builder is IBuilderWithEvaluator<TG, TS, TP> builderWithEvaluator) {
      builderWithEvaluator.Evaluator = builderWithEvaluator.Evaluator.ObserveWith(evaluatorObserver);
      hasAttached = true;
    }

    if (analysis is IInterceptorObserver<TG, TS, TP, TR> interceptorObserver && builder is IBuilderWithInterceptor<TG, TR, TS, TP> builderWithInterceptor) {
      var interceptor = builderWithInterceptor.Interceptor ?? new IdentityInterceptor<TG, TR>();
      builderWithInterceptor.Interceptor = interceptor.ObserveWith(interceptorObserver);
      hasAttached = true;
    }

    if (analysis is ITerminatorObserver<TG, TR, TS, TP> terminatorObserver && builder is IBuilderWithTerminator<TG, TR, TS, TP> builderWithTerminator) {
      builderWithTerminator.Terminator = builderWithTerminator.Terminator.ObserveWith(terminatorObserver);
      hasAttached = true;
    }

    if (analysis is ICreatorObserver<TG, TS, TP> cAnalysis && builder is IBuilderWithCreator<TG, TS, TP> builderWithCreator) {
      builderWithCreator.Creator = builderWithCreator.Creator.ObserveWith(cAnalysis);
      hasAttached = true;
    }

    if (analysis is ICrossoverObserver<TG, TS, TP> xAnalysis && builder is IBuilderWithCrossover<TG, TS, TP> builderWithCrossover) {
      builderWithCrossover.Crossover = builderWithCrossover.Crossover.ObserveWith(xAnalysis);
      hasAttached = true;
    }

    if (analysis is IMutatorObserver<TG, TS, TP> mAnalysis && builder is IBuilderWithMutator<TG, TS, TP> builderWithMutator) {
      builderWithMutator.Mutator = builderWithMutator.Mutator.ObserveWith(mAnalysis);
      hasAttached = true;
    }

    if (analysis is ISelectorObserver<TG, TS, TP> sAnalysis && builder is IBuilderWithSelector<TG, TS, TP> builderWithSelector) {
      builderWithSelector.Selector = builderWithSelector.Selector.ObserveWith(sAnalysis);
      hasAttached = true;
    }

    if (!hasAttached)
      throw new InvalidOperationException($"this observer {analysis} could not attach to {builder}");
  }

  public static IExecutionInstance RetrieveAnalysis(this IExecutable<IExecutionInstance> analysis, ExecutionInstanceRegistry instanceRegistry) => instanceRegistry.Resolve(analysis);
}
