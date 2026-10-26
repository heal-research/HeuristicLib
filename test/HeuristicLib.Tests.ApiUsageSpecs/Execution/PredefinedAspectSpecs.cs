using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Terminators;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public sealed class PredefinedAspectSpecs
{
    [Fact]
    public void PredefinedAspectsInferEveryRoleFromTheirArguments()
    {
        var creator = new UniformDistributedCreator();
        var crossover = SelectFirstParentCrossover<RealVector>.Instance;
        var evaluator = new ProblemEvaluator<RealVector>();
        var mutator = new GaussianMutator(0.5, 1.0);
        var refiner = NoChangeRefiner<RealVector>.Instance;
        var selector = new RandomSelector<RealVector>();
        var replacer = new CommaSelectionReplacer<RealVector>();
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var terminator = new NeverTerminator<RealVector>();
        var algorithm = GeneticAlgorithm.Create(creator, crossover, mutator);
        var counter = new CountAccumulator();
        var duration = new DurationAccumulator();
        var timeProvider = TimeProvider.System;
        (IConfigurationNode Source, Aspect Aspect)[] aspects =
        [
            (algorithm, Aspect.CountInvocations(algorithm, counter)),
            (algorithm, Aspect.MeasureDuration(algorithm, duration)),
            (algorithm, Aspect.MeasureDuration(algorithm, duration, timeProvider)),
            (creator, Aspect.CountInvocations(creator, counter)),
            (creator, Aspect.MeasureDuration(creator, duration)),
            (creator, Aspect.MeasureDuration(creator, duration, timeProvider)),
            (crossover, Aspect.CountInvocations(crossover, counter)),
            (crossover, Aspect.MeasureDuration(crossover, duration)),
            (crossover, Aspect.MeasureDuration(crossover, duration, timeProvider)),
            (evaluator, Aspect.CountInvocations(evaluator, counter)),
            (evaluator, Aspect.MeasureDuration(evaluator, duration)),
            (evaluator, Aspect.MeasureDuration(evaluator, duration, timeProvider)),
            (mutator, Aspect.CountInvocations(mutator, counter)),
            (mutator, Aspect.MeasureDuration(mutator, duration)),
            (mutator, Aspect.MeasureDuration(mutator, duration, timeProvider)),
            (refiner, Aspect.CountInvocations(refiner, counter)),
            (refiner, Aspect.MeasureDuration(refiner, duration)),
            (refiner, Aspect.MeasureDuration(refiner, duration, timeProvider)),
            (selector, Aspect.CountInvocations(selector, counter)),
            (selector, Aspect.MeasureDuration(selector, duration)),
            (selector, Aspect.MeasureDuration(selector, duration, timeProvider)),
            (replacer, Aspect.CountInvocations(replacer, counter)),
            (replacer, Aspect.MeasureDuration(replacer, duration)),
            (replacer, Aspect.MeasureDuration(replacer, duration, timeProvider)),
            (interceptor, Aspect.CountInvocations(interceptor, counter)),
            (interceptor, Aspect.MeasureDuration(interceptor, duration)),
            (interceptor, Aspect.MeasureDuration(interceptor, duration, timeProvider)),
            (terminator, Aspect.CountInvocations(terminator, counter)),
            (terminator, Aspect.MeasureDuration(terminator, duration)),
            (terminator, Aspect.MeasureDuration(terminator, duration, timeProvider)),
            (creator, Aspect.CountCandidates(creator, counter)),
            (creator, Aspect.CountCandidates(creator, out var creatorCount)),
            (crossover, Aspect.CountCandidates(crossover, counter)),
            (crossover, Aspect.CountCandidates(crossover, out var crossoverCount)),
            (evaluator, Aspect.CountCandidates(evaluator, counter)),
            (evaluator, Aspect.CountCandidates(evaluator, out var evaluatorCount)),
            (mutator, Aspect.CountCandidates(mutator, counter)),
            (mutator, Aspect.CountCandidates(mutator, out var mutatorCount)),
            (refiner, Aspect.CountCandidates(refiner, counter)),
            (refiner, Aspect.CountCandidates(refiner, out var refinerCount)),
            (selector, Aspect.CountCandidates(selector, counter)),
            (selector, Aspect.CountCandidates(selector, out var selectorCount)),
            (replacer, Aspect.CountCandidates(replacer, counter)),
            (replacer, Aspect.CountCandidates(replacer, out var replacerCount)),
            (algorithm, Aspect.Observe(algorithm.Iterations, _ => { })),
            (crossover, Aspect.Observe(crossover.Invocations, _ => { })),
            (evaluator, Aspect.Observe(evaluator.Invocations, _ => { })),
            (mutator, Aspect.Observe(mutator.Invocations, _ => { })),
            (interceptor, Aspect.Observe(interceptor.Invocations, _ => { }))
        ];

        foreach (var (source, aspect) in aspects)
            aspect.ShouldHaveSingleItem().Pointcut.Matches(source).ShouldBeTrue();

        Clock.FromIterations(algorithm).ShouldHaveSingleItem().Pointcut.Matches(algorithm).ShouldBeTrue();
        Clock.FromCandidates(evaluator).ShouldHaveSingleItem().Pointcut.Matches(evaluator).ShouldBeTrue();
        Clock.FromElapsedTime(timeProvider).ShouldBeOfType<ElapsedTimeClock>();
    }
}
