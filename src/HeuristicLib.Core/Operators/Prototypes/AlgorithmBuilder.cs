using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public abstract class AlgorithmBuilder<TGenotype, TEncoding, TProblem, TRes, TAlg> : IAlgorithmBuilder<TGenotype, TEncoding, TProblem, TRes, TAlg>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TRes : class, IAlgorithmState
  where TAlg : IAlgorithm<TGenotype, TEncoding, TProblem, TRes>
  where TGenotype : class {
  public required ICreator<TGenotype, TEncoding, TProblem> Creator { get; set; }
  public ITerminator<TGenotype, TRes, TEncoding, TProblem> Terminator { get; set; } = new AfterIterationsTerminator<TGenotype>(200);
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; set; } = new DirectEvaluator<TGenotype>();
  public int? RandomSeed { get; set; }
  public IInterceptor<TGenotype, TRes, TEncoding, TProblem>? Interceptor { get; set; }
  IAlgorithm<TGenotype, TEncoding, TProblem, TRes> IAlgorithmBuilder<TGenotype, TEncoding, TProblem, TRes>.BuildAlgorithm() => BuildAlgorithm();

  public TRes Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null) {
    return BuildAlgorithm().Execute(problem, searchSpace, random);
  }

  public void Add(IAlgorithmAttachment<TGenotype, TEncoding, TProblem, TRes> attachment) {
    attachment.AttachTo(this);
  }

  public abstract TAlg BuildAlgorithm();
}
