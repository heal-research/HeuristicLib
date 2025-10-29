using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public class StochasticEvaluator<TGenotype> : StochasticEvaluator<TGenotype, IEncoding<TGenotype>, IStochasticProblem<TGenotype, IEncoding<TGenotype>>>;
