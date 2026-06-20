using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public record TargetTerminator<TGenotype>
  : StatelessTerminator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, PopulationState<TGenotype>>
{
    public ObjectiveVector Target { get; init; }

    public TargetTerminator(ObjectiveVector target)
    {
        Target = target;
    }

    public override bool IsTerminalState(PopulationState<TGenotype> state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
      => TargetTerminator.IsTerminalState(state, problem, Target);
}

public static class TargetTerminator
{
    public static bool IsTerminalState<TGenotype>(
      PopulationState<TGenotype> state,
      IProblem<TGenotype, ISearchSpace<TGenotype>> problem,
      ObjectiveVector target)
    {
        return state.Population.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
    }
}
