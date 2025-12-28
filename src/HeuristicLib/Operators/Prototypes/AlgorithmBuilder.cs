using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public abstract class AlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes, TAlg> : IAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes, TAlg>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TRes : class, IIterationResult
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
