using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record GeneticAlgorithmBuilder<TCandidate, TSearchSpace, TProblem>
  : AlgorithmBuilder<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>, GeneticAlgorithm<TCandidate, TSearchSpace, TProblem>>,
    IBuilderWithCreator<TCandidate, TSearchSpace, TProblem>,
    IBuilderWithSelector<TCandidate, TSearchSpace, TProblem>,
    IBuilderWithCrossover<TCandidate, TSearchSpace, TProblem>,
    IBuilderWithMutator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public int PopulationSize { get; set; } = 100;
    public ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; set; } = new TournamentSelector<TCandidate>(2);

    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; set; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; set; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; set; }
    public double MutationRate { get; set; } = 0.05;

    public int Elites { get; set; } = 1;

    public override GeneticAlgorithm<TCandidate, TSearchSpace, TProblem> Build()
    {
        return new GeneticAlgorithm<TCandidate, TSearchSpace, TProblem>
        {
            PopulationSize = PopulationSize,
            Creator = Creator,
            Crossover = Crossover,
            Selector = Selector,
            Evaluator = Evaluator,
            Elites = Elites,
            Interceptor = Interceptor,
            Mutator = Mutator,
            MutationRate = MutationRate
        };
    }
}
