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
        (IConfigurationNode Source, ISubstitutionBundle Analyzer)[] analyzers =
        [
            (algorithm, algorithm.Calls.Count()),
            (creator, creator.Invocations.Count()),
            (creator, creator.Invocations.Duration(timeProvider)),
            (crossover, crossover.Invocations.Count()),
            (crossover, crossover.Invocations.Duration(timeProvider)),
            (evaluator, evaluator.Invocations.Count()),
            (evaluator, evaluator.Invocations.Duration(timeProvider)),
            (mutator, mutator.Invocations.Count()),
            (mutator, mutator.Invocations.Duration(timeProvider)),
            (refiner, refiner.Invocations.Count()),
            (refiner, refiner.Invocations.Duration(timeProvider)),
            (selector, selector.Invocations.Count()),
            (selector, selector.Invocations.Duration(timeProvider)),
            (replacer, replacer.Invocations.Count()),
            (replacer, replacer.Invocations.Duration(timeProvider)),
            (interceptor, interceptor.Invocations.Count()),
            (interceptor, interceptor.Invocations.Duration(timeProvider)),
            (terminator, terminator.Invocations.Count()),
            (terminator, terminator.Invocations.Duration(timeProvider)),
            (creator, creator.Invocations.CountCandidates()),
            (crossover, crossover.Invocations.CountCandidates()),
            (evaluator, evaluator.Invocations.CountCandidates()),
            (mutator, mutator.Invocations.CountCandidates()),
            (refiner, refiner.Invocations.CountCandidates()),
            (selector, selector.Invocations.CountCandidates()),
            (replacer, replacer.Invocations.CountCandidates()),
            (algorithm, algorithm.Iterations.Count())
        ];
        (IConfigurationNode Source, Aspect Aspect)[] aspects =
        [
            (algorithm, Aspect.Observe(algorithm.Iterations, _ => { })),
            (crossover, Aspect.Observe(crossover.Invocations, _ => { })),
            (evaluator, Aspect.Observe(evaluator.Invocations, _ => { })),
            (mutator, Aspect.Observe(mutator.Invocations, _ => { })),
            (interceptor, Aspect.Observe(interceptor.Invocations, _ => { }))
        ];

        foreach (var (source, analyzer) in analyzers)
            source.Substitute(analyzer).ShouldNotBeSameAs(source);
        foreach (var (source, aspect) in aspects)
            aspect.ShouldHaveSingleItem().Pointcut.Matches(source).ShouldBeTrue();

        results.Elapsed.ShouldBe(TimeSpan.Zero);
    }
}
