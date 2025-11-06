using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public class Prototype<TGenotype, TEncoding, TProblem, TRes>(
  ICreator<TGenotype, TEncoding, TProblem> creator,
  ITerminator<TGenotype, TRes, TEncoding, TProblem> terminator,
  IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
  int? randomSeed,
  IInterceptor<TGenotype, TRes, TEncoding, TProblem>? interceptor,
  params IAnalyzer<TGenotype, TRes, TEncoding, TProblem>[] analyzers)
  : IPrototype<TGenotype, TEncoding, TProblem, TRes> where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> where TRes : IIterationResult {
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; set; } = creator;
  public ITerminator<TGenotype, TRes, TEncoding, TProblem> Terminator { get; set; } = terminator;
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; set; } = evaluator;
  public int? RandomSeed { get; set; } = randomSeed;
  public IInterceptor<TGenotype, TRes, TEncoding, TProblem>? Interceptor { get; set; } = interceptor;
  public IAnalyzer<TGenotype, TRes, TEncoding, TProblem>[] Analyzers { get; set; } = analyzers;
}
