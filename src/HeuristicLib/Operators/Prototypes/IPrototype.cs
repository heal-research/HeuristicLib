using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public interface IPrototype<TGenotype, TEncoding, TProblem, TRes>
  where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> where TRes : IIterationResult {
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; set; }
  public ITerminator<TGenotype, TRes, TEncoding, TProblem> Terminator { get; set; }
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; set; }
  public IInterceptor<TGenotype, TRes, TEncoding, TProblem>? Interceptor { get; set; }
}
