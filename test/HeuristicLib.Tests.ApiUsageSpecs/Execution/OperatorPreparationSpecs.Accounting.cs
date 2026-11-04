using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public sealed partial class OperatorPreparationSpecs
{
    [Fact]
    public async Task PerRunAdviceKeepsSharedHandleAccountingIndependent()
    {
        var source = new CountingSource();
        var count = new CountAccumulatorHandle();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = new TwoOperationAlgorithm(source).TerminatedBy(AfterOperatorCountTerminator.For(problem, count, 2));
        var mutator = (IOperator<Mutation, RealVector>)source;
        var runs = Enumerable.Range(0, 3).Select(_ => algorithm.CreateRun(problem, RandomNumberGenerator.Create(1), setup =>
        {
            var counter = new CountAccumulator();
            setup.AddResource(count, counter).AddAspect(mutator.CountInvocations(counter));
        })).ToArray();

        await Task.WhenAll(runs.Select(run => run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken)));

        var counts = runs.Select(run => run.Resource(count)).ToArray();
        counts.ShouldAllBe(accumulator => accumulator.CurrentCount == 2);
        counts.Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(3);
    }

    [Fact]
    public void CandidateAccountingAndTheTerminatorUseTheSuppliedResource()
    {
        var source = new CountingSource();
        var count = new CountAccumulatorHandle();
        var supplied = new CountAccumulator();
        supplied.IncrementBy(3);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = new TwoOperationAlgorithm(source).TerminatedBy(new AfterOperatorCountTerminator<RealVector>(count, 5));
        var counting = ((IOperator<Mutation, RealVector>)source).CountCandidates(supplied);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1),
            setup => setup.AddResource(count, supplied).AddAspect(counting));

        run.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        run.Resource(count).ShouldBeSameAs(supplied);
        supplied.CurrentCount.ShouldBe(5);

        var next = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1),
            setup => setup.AddResource(count, supplied).AddAspect(counting));
        next.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(1);
        supplied.CurrentCount.ShouldBe(6);
    }

    [Fact]
    public void DurationAccountingAndTheTerminatorUseOneResourcePerRun()
    {
        var source = new CountingSource();
        var duration = new DurationAccumulatorHandle();
        var clock = new BudgetClock();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = new TwoOperationAlgorithm(source).TerminatedBy(
            AfterOperatorDurationTerminator.For(problem, duration, TimeSpan.FromTicks(2)));
        var mutator = (IOperator<Mutation, RealVector>)source;
        void Configure(AlgorithmRunSetup<RealVector, SingleSolutionState<RealVector>> setup)
        {
            var elapsed = new DurationAccumulator();
            setup.AddResource(duration, elapsed).AddAspect(mutator.MeasureDuration(elapsed, clock));
        }
        var first = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1), Configure);
        var second = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1), Configure);

        first.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        second.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        first.Resource(duration).CurrentDuration.ShouldBe(TimeSpan.FromTicks(2));
        second.Resource(duration).CurrentDuration.ShouldBe(TimeSpan.FromTicks(2));
        first.Resource(duration).ShouldNotBeSameAs(second.Resource(duration));
    }

    [Fact]
    public void FluentCountingNeedsNoResourceBinding()
    {
        var source = new CountingSource();
        var mutator = (IOperator<Mutation, RealVector>)source;
        var counting = mutator.CountInvocations(out var counter);
        counter.CurrentCount.ShouldBe(0);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var run = new TwoOperationAlgorithm(source).CreateRun(problem, RandomNumberGenerator.Create(1),
            setup => setup.AddAspect(counting));

        run.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(4);
        counter.CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void FluentCandidateMetricRejectsUnsupportedOperations()
    {
        var source = new CountingSource();
        var counter = new CountAccumulator();
        var counting = ((IOperator<ReadValue, RealVector>)source).CountCandidates(counter);
        var root = ResolutionScope.Create();
        root.AddAspect(counting);
        var scope = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

        Should.Throw<InvalidOperationException>(() => Read(scope, source)).Message.ShouldContain("reports none");
        counter.CurrentCount.ShouldBe(0);
    }
}
