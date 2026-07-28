using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public record TargetTerminator<TCandidate>
  : StatelessTerminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, PopulationState<TCandidate>>
{
    public ObjectiveVector Target { get; init; }

    public TargetTerminator(ObjectiveVector target)
    {
        Target = target;
    }

    public override bool IsTerminalState(PopulationState<TCandidate> state, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
      => TargetTerminator.IsTerminalState(state, problem, Target);
}

public static class TargetTerminator
{
    public static TargetTerminator<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, ObjectiveVector target)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new(target);

    public static bool IsTerminalState<TCandidate>(
      PopulationState<TCandidate> state,
      IProblem<TCandidate, ISearchSpace<TCandidate>> problem,
      ObjectiveVector target)
    {
        return state.Population.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
    }
}
