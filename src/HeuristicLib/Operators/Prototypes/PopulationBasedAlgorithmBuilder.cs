using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public abstract class PopulationBasedAlgorithmBuilder<TGenotype, TEncoding, TProblem, TRes, TAlg>() : AlgorithmBuilder<TGenotype, TEncoding, TProblem, TRes, TAlg>(),
                                                                                                      ISelectorPrototype<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TRes : PopulationIterationResult<TGenotype>
  where TAlg : IAlgorithm<TGenotype, TEncoding, TProblem, TRes>
  where TGenotype : class {
  public int PopulationSize { get; set; } = 100;
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; set; } = new TournamentSelector<TGenotype>(2);
}
