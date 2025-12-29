using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.OperatorPrototypes;

public abstract class AlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes, TAlg> : IAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes, TAlg>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TRes : class, IIterationState
  where TAlg : IAlgorithm<TGenotype, TSearchSpace, TProblem, TRes>
  where TGenotype : class {
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; set; }
  public ITerminator<TGenotype, TRes, TSearchSpace, TProblem> Terminator { get; set; } = new AfterIterationsTerminator<TGenotype>(200);
  public IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; set; } = new DirectEvaluator<TGenotype>();
  public int? RandomSeed { get; set; }
  public IInterceptor<TGenotype, TRes, TSearchSpace, TProblem>? Interceptor { get; set; }
  IAlgorithm<TGenotype, TSearchSpace, TProblem, TRes> IAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes>.BuildAlgorithm() => BuildAlgorithm();

  public TRes Execute(TProblem problem, TSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null) {
    return BuildAlgorithm().Execute(problem, searchSpace, random);
  }

  public void Add(IAlgorithmAttachment<TGenotype, TSearchSpace, TProblem, TRes> attachment) {
    attachment.AttachTo(this);
  }

  public abstract TAlg BuildAlgorithm();
}
