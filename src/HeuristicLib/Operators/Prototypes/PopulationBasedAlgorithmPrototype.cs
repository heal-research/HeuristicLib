using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public abstract class PopulationBasedAlgorithmPrototype<TGenotype, TEncoding, TProblem, TRes>(
  int populationSize,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  ISelector<TGenotype, TEncoding, TProblem> selector,
  IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
  int? randomSeed,
  ITerminator<TGenotype, TRes, TEncoding, TProblem> terminator,
  IInterceptor<TGenotype, TRes, TEncoding, TProblem>? interceptor = null) : Prototype<TGenotype, TEncoding, TProblem, TRes>(creator, terminator, evaluator, randomSeed, interceptor),
                                                                            ISelectorPrototype<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> where TRes : PopulationIterationResult<TGenotype> {
  public int PopulationSize { get; set; } = populationSize;
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; set; } = selector;
}
