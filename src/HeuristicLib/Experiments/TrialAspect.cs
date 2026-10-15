using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Experiments;

public sealed record TrialAspect<TTrial, TAspect>(TTrial Trial, TAspect Aspect)
    where TAspect : Aspect;

public static class TrialAspect
{
    public static TrialAspect<TTrial, TAspect> From<TTrial, TAspect>(TTrial trial, TAspect aspect)
        where TAspect : Aspect => new(trial, aspect);
}
