using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record EvolutionStrategyBuilder<TCandidate, TSearchSpace, TProblem>
    : AlgorithmBuilder<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>, EvolutionStrategy<TCandidate, TSearchSpace, TProblem>>,
      IBuilderWithCreator<TCandidate, TSearchSpace, TProblem>,
      IBuilderWithMutator<TCandidate, TSearchSpace, TProblem>,
      IBuilderWithSelector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public int PopulationSize { get; set; } = 100;
    public EvolutionStrategyType Strategy { get; set; } = EvolutionStrategyType.Plus;
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; set; }
    public ICrossover<TCandidate, TSearchSpace, TProblem>? Crossover { get; set; }
    public ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; set; } = new RandomSelector<TCandidate>();
    public int NumberOfChildren { get; set; } = 100;
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; set; }

    public override EvolutionStrategy<TCandidate, TSearchSpace, TProblem> Build() => new()
    {
        PopulationSize = PopulationSize,
        Strategy = Strategy,
        Creator = Creator,
        Mutator = Mutator,
        Crossover = Crossover,
        Selector = Selector,
        Evaluator = Evaluator,
        Interceptor = Interceptor,
        NumberOfChildren = NumberOfChildren
    };
}
