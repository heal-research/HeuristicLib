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
        var results = ResolutionScope.Create().Results;
        var timeProvider = TimeProvider.System;
        (IConfigurationNode Source, Aspect Aspect)[] aspects =
        [
            (algorithm, results.Aspect(algorithm.Calls.Count())),
            (creator, results.Aspect(creator.Invocations.Count())),
            (creator, results.Aspect(creator.Invocations.Duration(timeProvider))),
            (crossover, results.Aspect(crossover.Invocations.Count())),
            (crossover, results.Aspect(crossover.Invocations.Duration(timeProvider))),
            (evaluator, results.Aspect(evaluator.Invocations.Count())),
            (evaluator, results.Aspect(evaluator.Invocations.Duration(timeProvider))),
            (mutator, results.Aspect(mutator.Invocations.Count())),
            (mutator, results.Aspect(mutator.Invocations.Duration(timeProvider))),
            (refiner, results.Aspect(refiner.Invocations.Count())),
            (refiner, results.Aspect(refiner.Invocations.Duration(timeProvider))),
            (selector, results.Aspect(selector.Invocations.Count())),
            (selector, results.Aspect(selector.Invocations.Duration(timeProvider))),
            (replacer, results.Aspect(replacer.Invocations.Count())),
            (replacer, results.Aspect(replacer.Invocations.Duration(timeProvider))),
            (interceptor, results.Aspect(interceptor.Invocations.Count())),
            (interceptor, results.Aspect(interceptor.Invocations.Duration(timeProvider))),
            (terminator, results.Aspect(terminator.Invocations.Count())),
            (terminator, results.Aspect(terminator.Invocations.Duration(timeProvider))),
            (creator, results.Aspect(creator.Invocations.CountCandidates())),
            (crossover, results.Aspect(crossover.Invocations.CountCandidates())),
            (evaluator, results.Aspect(evaluator.Invocations.CountCandidates())),
            (mutator, results.Aspect(mutator.Invocations.CountCandidates())),
            (refiner, results.Aspect(refiner.Invocations.CountCandidates())),
            (selector, results.Aspect(selector.Invocations.CountCandidates())),
            (replacer, results.Aspect(replacer.Invocations.CountCandidates())),
            (algorithm, Aspect.Observe(algorithm.Iterations, _ => { })),
            (crossover, Aspect.Observe(crossover.Invocations, _ => { })),
            (evaluator, Aspect.Observe(evaluator.Invocations, _ => { })),
            (mutator, Aspect.Observe(mutator.Invocations, _ => { })),
            (interceptor, Aspect.Observe(interceptor.Invocations, _ => { }))
        ];

        foreach (var (source, aspect) in aspects)
            aspect.ShouldHaveSingleItem().Pointcut.Matches(source).ShouldBeTrue();

        // Counters are instruments: each run creates the aspect that places them at their source.
        results.Aspect(algorithm.Iterations.Count()).ShouldHaveSingleItem().Pointcut.Matches(algorithm).ShouldBeTrue();
        results.Aspect(evaluator.Invocations.CountCandidates()).ShouldHaveSingleItem().Pointcut.Matches(evaluator).ShouldBeTrue();
        results.Aspect(evaluator.Invocations.Duration(timeProvider)).ShouldHaveSingleItem().Pointcut.Matches(evaluator).ShouldBeTrue();
        results.Elapsed.ShouldBe(TimeSpan.Zero);
    }
}
