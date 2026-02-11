using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

// shortening interfaces for simple cases where the search space and problem are not relevant for the analysis

public interface IInterceptorObserver<TGenotype> :
  IInterceptorObserver<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>;

public interface IInterceptorObserver<TGenotype, in TState> :
  IInterceptorObserver<TGenotype, TState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TState : class, IAlgorithmState;

public interface IEvaluatorObserver<TGenotype> :
  IEvaluatorObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>;

public interface ICrossoverObserver<TGenotype> :
  ICrossoverObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>;

public interface IMutatorObserver<TGenotype> :
  IMutatorObserver<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>;

public static class BuilderExtensions
{
  public static void AttachObserver<TG, TS, TP, TR, TAlg, TBuildSpec>(this AlgorithmBuilder<TG, TS, TP, TR, TAlg, TBuildSpec> builder, IExecutable<IExecutionInstance> analysis) where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
    where TAlg : IAlgorithm<TG, TS, TP, TR>
    where TBuildSpec : AlgorithmBuildSpec<TG, TS, TP, TR>
  {
    var spec = builder.CreateBuildSpec();
    spec.AttachObserver(analysis);
    builder.BuildFromSpec(spec);
  }

  public static void AttachObserver<TG, TR, TS, TP>(this AlgorithmBuildSpec<TG, TS, TP, TR> spec, IExecutable<IExecutionInstance> analysis)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : class, IAlgorithmState
  {
    var t = false;
    if (analysis is IEvaluatorObserver<TG, TS, TP> evaluatorObserver) {
      spec.Evaluator = spec.Evaluator.ObserveWith(evaluatorObserver);
      t = true;
    }

    if (analysis is IInterceptorObserver<TG, TR, TS, TP> interceptorObserver) {
      var interceptor = spec.Interceptor ?? new IdentityInterceptor<TG, TR>();
      spec.Interceptor = interceptor.ObserveWith(interceptorObserver);
      t = true;
    }

    // if (analysis is ITerminatorObserver<TG, TR, TS, TP> terminatorObserver) {
    //   spec.Terminator = spec.Terminator.ObserveWith(terminatorObserver);
    //   t = true;
    // }

    if (analysis is ICreatorObserver<TG, TS, TP> cAnalysis && spec is ISpecWithCreator<TG, TS, TP> cspec) {
      cspec.Creator = cspec.Creator.ObserveWith(cAnalysis);
      t = true;
    }

    if (analysis is ICrossoverObserver<TG, TS, TP> xAnalysis && spec is ISpecWithCrossover<TG, TS, TP> xSpec) {
      xSpec.Crossover = xSpec.Crossover.ObserveWith(xAnalysis);
      t = true;
    }

    if (analysis is IMutatorObserver<TG, TS, TP> mAnalysis && spec is ISpecWithMutator<TG, TS, TP> mSpec) {
      mSpec.Mutator = mSpec.Mutator.ObserveWith(mAnalysis);
      t = true;
    }

    if (analysis is ISelectorObserver<TG, TS, TP> sAnalysis && spec is ISpecWithSelector<TG, TS, TP> sSpec) {
      sSpec.Selector = sSpec.Selector.ObserveWith(sAnalysis);
      t = true;
    }

    if (!t)
      throw new InvalidOperationException($"this observer {analysis} could not attach to {spec}");
  }

  public static IExecutionInstance RetrieveAnalysis(this IExecutable<IExecutionInstance> analysis, ExecutionInstanceRegistry instanceRegistry) => instanceRegistry.Resolve(analysis);
}
