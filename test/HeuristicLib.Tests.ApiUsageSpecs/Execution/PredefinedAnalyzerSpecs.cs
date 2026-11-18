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

public sealed class PredefinedAnalyzerSpecs
{
    [Fact]
    public void PredefinedPlacementsInferEveryRoleFromTheirArguments()
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
        (IConfigurationNode Source, ISubstitution Placement)[] placements =
        [
            (algorithm, algorithm.Calls.Counting(new Counter())),
            (creator, creator.Invocations.Counting(new Counter())),
            (creator, creator.Invocations.Timing(new Duration())),
            (crossover, crossover.Invocations.Counting(new Counter())),
            (crossover, crossover.Invocations.Timing(new Duration())),
            (evaluator, evaluator.Invocations.Counting(new Counter())),
            (evaluator, evaluator.Invocations.Timing(new Duration())),
            (mutator, mutator.Invocations.Counting(new Counter())),
            (mutator, mutator.Invocations.Timing(new Duration())),
            (refiner, refiner.Invocations.Counting(new Counter())),
            (refiner, refiner.Invocations.Timing(new Duration())),
            (selector, selector.Invocations.Counting(new Counter())),
            (selector, selector.Invocations.Timing(new Duration())),
            (replacer, replacer.Invocations.Counting(new Counter())),
            (replacer, replacer.Invocations.Timing(new Duration())),
            (interceptor, interceptor.Invocations.Counting(new Counter())),
            (interceptor, interceptor.Invocations.Timing(new Duration())),
            (terminator, terminator.Invocations.Counting(new Counter())),
            (terminator, terminator.Invocations.Timing(new Duration())),
            (creator, creator.Invocations.CountingCandidates(new Counter())),
            (crossover, crossover.Invocations.CountingCandidates(new Counter())),
            (evaluator, evaluator.Invocations.CountingCandidates(new Counter())),
            (mutator, mutator.Invocations.CountingCandidates(new Counter())),
            (refiner, refiner.Invocations.CountingCandidates(new Counter())),
            (selector, selector.Invocations.CountingCandidates(new Counter())),
            (replacer, replacer.Invocations.CountingCandidates(new Counter())),
            (algorithm, algorithm.Iterations.Counting(new Counter())),
            (algorithm, algorithm.Iterations.Observing(_ => { })),
            (crossover, crossover.Invocations.Observing(_ => { })),
            (evaluator, evaluator.Invocations.Observing(_ => { })),
            (mutator, mutator.Invocations.Observing(_ => { })),
            (interceptor, interceptor.Invocations.Observing(_ => { }))
        ];

        foreach (var (source, placement) in placements)
            source.Substitute(placement).ShouldNotBeSameAs(source);

        results.Elapsed.ShouldBe(TimeSpan.Zero);
    }
}
