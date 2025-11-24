using System.Text;
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

public abstract class Prototype<TGenotype, TEncoding, TProblem, TRes> : IPrototype<TGenotype, TEncoding, TProblem, TRes>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TRes : IIterationResult {
  protected Prototype(ICreator<TGenotype, TEncoding, TProblem> creator,
                      ITerminator<TGenotype, TRes, TEncoding, TProblem> terminator,
                      IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
                      int? randomSeed,
                      IInterceptor<TGenotype, TRes, TEncoding, TProblem>? interceptor) {
    Creator = creator;
    Terminator = terminator;
    Evaluator = evaluator;
    RandomSeed = randomSeed;
    Interceptor = interceptor;
  }

  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; set; }
  public ITerminator<TGenotype, TRes, TEncoding, TProblem> Terminator { get; set; }
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; set; }
  public int? RandomSeed { get; set; }
  public IInterceptor<TGenotype, TRes, TEncoding, TProblem>? Interceptor { get; set; }

  public TRes Execute(TProblem problem, TEncoding? searchSpace = null, IRandomNumberGenerator? random = null) {
    return BuildAlgorithm().Execute(problem, searchSpace, random);
  }

  protected abstract IIterativeAlgorithm<TGenotype, TEncoding, TProblem, TRes> BuildAlgorithm();
}
