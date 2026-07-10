using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

public record GenealogyAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState> :
  Analyzer<TCandidate, TSearchSpace, TProblem, TSearchState, GenealogyGraph<TCandidate>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : PopulationState<TCandidate>
  where TCandidate : notnull
{
    private readonly IEqualityComparer<TCandidate>? equality;
    private readonly bool saveSpace;

    public GenealogyAnalysis(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm,
                             ICrossover<TCandidate, TSearchSpace, TProblem>? crossover = null,
                             IMutator<TCandidate, TSearchSpace, TProblem>? mutator = null,
                             IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor = null,
                             IEqualityComparer<TCandidate>? equality = null,
                             bool saveSpace = false) : base(Algorithm)
    {
        this.equality = equality;
        this.saveSpace = saveSpace;
        Crossover = crossover;
        Mutator = mutator;
        Interceptor = interceptor;
    }

    private ICrossover<TCandidate, TSearchSpace, TProblem>? Crossover { get; }
    private IMutator<TCandidate, TSearchSpace, TProblem>? Mutator { get; }
    private IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>? Interceptor { get; }

    public override void RegisterObservations(ObservationPlan observations, GenealogyGraph<TCandidate> graph)
    {
        if (Crossover is not null)
        {
            observations.Observe(Crossover, ((offspring, parents, _, _) => AfterCross(graph, offspring, parents)));
        }

        if (Mutator is not null)
        {
            observations.Observe(Mutator, ((offspring, parent, _, _) => AfterMutate(graph, offspring, parent)));
        }

        if (Interceptor is not null)
        {
            observations.Observe(Interceptor, ((currentState, _, _, _, problem) => AfterInterception(graph, currentState, problem)));
        }
    }

    public void AfterCross(GenealogyGraph<TCandidate> graph, IReadOnlyList<TCandidate> offspring, IReadOnlyList<IParents<TCandidate>> parents)
    {
        foreach (var (parents1, child) in parents.Zip(offspring))
        {
            graph.AddConnection([parents1.Item1, parents1.Item2], child);
        }
    }

    public void AfterMutate(GenealogyGraph<TCandidate> graph, IReadOnlyList<TCandidate> offspring, IReadOnlyList<TCandidate> parent)
    {
        foreach (var (parents1, child) in parent.Zip(offspring))
        {
            graph.AddConnection([parents1], child);
        }
    }

    public void AfterInterception(GenealogyGraph<TCandidate> graph, TSearchState currentState, TProblem problem)
    {
        var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
        graph.SetAsNewGeneration(ordered.Select(x => x.Candidate), saveSpace);
    }

    public override GenealogyGraph<TCandidate> CreateInitialResult() => new(equality ?? EqualityComparer<TCandidate>.Default);
}
