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
    public async Task EachRunCountsIntoItsOwnResultOfOneCounter()
    {
        var source = new CountingSource();
        var mutator = (IOperator<Mutation, RealVector>)source;
        var counter = mutator.Invocations.Count();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = new TwoOperationAlgorithm(source).TerminatedBy(AfterOperatorCountTerminator.For(problem, counter, 2)).Advised(counter);
        var runs = Enumerable.Range(0, 3).Select(_ => algorithm.CreateRun(problem, RandomNumberGenerator.Create(1))).ToArray();

        await Task.WhenAll(runs.Select(run => run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken)));

        var counts = runs.Select(run => run.Result(counter)).ToArray();
        counts.ShouldAllBe(accumulator => accumulator.CurrentCount == 2);
        counts.Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(3);
    }

    [Fact]
    public void CandidateAccountingAndTheTerminatorUseTheRunsCounter()
    {
        var source = new CountingSource();
        var candidates = ((IOperator<Mutation, RealVector>)source).Invocations.CountCandidates();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = new TwoOperationAlgorithm(source).TerminatedBy(new AfterOperatorCountTerminator<RealVector>(candidates, 2)).Advised(candidates);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1));
        run.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        run.Result(candidates).CurrentCount.ShouldBe(2);

        var next = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1));
        next.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        next.Result(candidates).CurrentCount.ShouldBe(2);
    }

    [Fact]
    public void DurationAccountingAndTheTerminatorUseOneResultPerRun()
    {
        var source = new CountingSource();
        var clock = new BudgetClock();
        var duration = ((IOperator<Mutation, RealVector>)source).Invocations.Duration(clock);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = new TwoOperationAlgorithm(source).TerminatedBy(
            AfterOperatorDurationTerminator.For(problem, duration, TimeSpan.FromTicks(2))).Advised(duration);
        var first = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1));
        var second = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1));

        first.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        second.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        first.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromTicks(2));
        second.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromTicks(2));
        first.Result(duration).ShouldNotBeSameAs(second.Result(duration));
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
