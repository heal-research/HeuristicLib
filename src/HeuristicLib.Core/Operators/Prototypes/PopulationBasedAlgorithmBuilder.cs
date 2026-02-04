// using HEAL.HeuristicLib.Algorithms;
// using HEAL.HeuristicLib.Operators.Selectors;
// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Problems;
// using HEAL.HeuristicLib.SearchSpaces;
//
// namespace HEAL.HeuristicLib.Operators.Prototypes;
//
// public abstract class PopulationBasedAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes, TAlg> : AlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes, TAlg>//,
//   //ISelectorPrototype<TGenotype, TSearchSpace, TProblem>
//   where TGenotype : class
//   where TSearchSpace : class, ISearchSpace<TGenotype>
//   where TProblem : class, IProblem<TGenotype, TSearchSpace>
//   where TRes : PopulationAlgorithmState<TGenotype>
//   where TAlg : class, IAlgorithm<TGenotype, TSearchSpace, TProblem, TRes>
//   
// {
//   public int PopulationSize { get; set; } = 100;
//   public ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; set; } = new TournamentSelector<TGenotype>(2);
// }
