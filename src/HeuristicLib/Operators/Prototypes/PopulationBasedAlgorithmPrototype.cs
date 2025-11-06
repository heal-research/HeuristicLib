using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public class PopulationBasedAlgorithmPrototype<TGenotype, TEncoding, TProblem, TRes>(
  int populationSize,
  ICreator<TGenotype, TEncoding, TProblem> creator,
  ICrossover<TGenotype, TEncoding, TProblem> crossover,
  IMutator<TGenotype, TEncoding, TProblem> mutator,
  ISelector<TGenotype, TEncoding, TProblem> selector,
  IEvaluator<TGenotype, TEncoding, TProblem> evaluator,
  int? randomSeed,
  ITerminator<TGenotype, TRes, TEncoding, TProblem> terminator,
  IInterceptor<TGenotype, TRes, TEncoding, TProblem>? interceptor = null,
  params IAnalyzer<TGenotype, TRes, TEncoding, TProblem>[] analyzers
) : Prototype<TGenotype, TEncoding, TProblem, TRes>(creator, terminator, evaluator, randomSeed, interceptor, analyzers),
    ICrossoverPrototype<TGenotype, TEncoding, TProblem>,
    IMutatorPrototype<TGenotype, TEncoding, TProblem>,
    ISelectorPrototype<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype> where TProblem : class, IProblem<TGenotype, TEncoding> where TRes : PopulationIterationResult<TGenotype> {
  public int PopulationSize { get; set; } = populationSize;
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; set; } = crossover;
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; set; } = mutator;
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; set; } = selector;
}
