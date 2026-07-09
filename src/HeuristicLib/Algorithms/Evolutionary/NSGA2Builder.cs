using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

#pragma warning disable S101
// ReSharper disable once IdentifierTypo
// ReSharper disable once InconsistentNaming
public record NSGA2Builder<TCandidate, TSearchSpace, TProblem>
    : AlgorithmBuilder<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>, NSGA2<TCandidate, TSearchSpace, TProblem>>,
      IBuilderWithCreator<TCandidate, TSearchSpace, TProblem>,
      IBuilderWithSelector<TCandidate, TSearchSpace, TProblem>,
      IBuilderWithCrossover<TCandidate, TSearchSpace, TProblem>,
      IBuilderWithMutator<TCandidate, TSearchSpace, TProblem>
#pragma warning restore S101
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public int PopulationSize { get; set; } = 100;
    public ISelector<TCandidate, TSearchSpace, TProblem> Selector { get; set; } = new ParetoCrowdingTournamentSelector<TCandidate>(false);

    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; set; }
    public required ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; set; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; set; }
    public double MutationRate { get; set; } = 0.05;

    public override NSGA2<TCandidate, TSearchSpace, TProblem> Build() => new()
    {
        PopulationSize = PopulationSize,
        Creator = Creator,
        Crossover = Crossover,
        Selector = Selector,
        Evaluator = Evaluator,
        Replacer = new ParetoCrowdingReplacer<TCandidate>(true),
        Interceptor = Interceptor,
        Mutator = Mutator.WithRate(MutationRate)
    };
}
