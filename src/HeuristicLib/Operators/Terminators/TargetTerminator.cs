using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record TargetTerminator<TCandidate>
    : StatelessTerminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, PopulationState<TCandidate>>
{
    public ObjectiveVector Target { get; init; }

    public static Parameter<TargetTerminator<TCandidate>, ObjectiveVector> TargetParameter { get; } =
        new(nameof(Target), static terminator => terminator.Target, static (terminator, value) => terminator with { Target = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [TargetParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public TargetTerminator(ObjectiveVector target)
    {
        Target = target;
    }

    public override bool IsTerminalState(PopulationState<TCandidate> state, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        TargetTerminator.IsTerminalState(state, problem, Target);
}

public static class TargetTerminator
{
    public static TargetTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, ObjectiveVector target) => new(target);

    public static bool IsTerminalState<TCandidate>(PopulationState<TCandidate> state, IProblem<TCandidate, ISearchSpace<TCandidate>> problem, ObjectiveVector target)
    {
        return state.Population.EvaluatedCandidates.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
    }
}
