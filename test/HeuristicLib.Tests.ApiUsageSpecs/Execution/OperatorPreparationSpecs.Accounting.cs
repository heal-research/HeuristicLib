using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
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
        var counter = new Counter();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = new TwoOperationAlgorithm(source).TerminatedBy(AfterOperatorCountTerminator.For(problem, counter, 2)).Substitute(mutator.Invocations.Counting(counter));
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
        var candidates = new Counter();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = new TwoOperationAlgorithm(source).TerminatedBy(new AfterOperatorCountTerminator<RealVector>(candidates, 2)).Substitute(((IOperator<Mutation, RealVector>)source).Invocations.CountingCandidates(candidates));

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
        var duration = new Duration();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = new TwoOperationAlgorithm(source).TerminatedBy(
            AfterOperatorDurationTerminator.For(problem, duration, TimeSpan.Zero)).Substitute(((IOperator<Mutation, RealVector>)source).Invocations.Timing(duration));
        var first = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1));
        var second = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1));
        var firstDuration = first.Result(duration);
        var secondDuration = second.Result(duration);

        first.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(1);
        second.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(1);
        first.Result(duration).ShouldBeSameAs(firstDuration);
        second.Result(duration).ShouldBeSameAs(secondDuration);
        first.Result(duration).ShouldNotBeSameAs(second.Result(duration));
    }

    [Fact]
    public void FluentCountingNeedsNoResourceBinding()
    {
        var source = new CountingSource();
        var mutator = (IOperator<Mutation, RealVector>)source;
        var counter = new Counter();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var run = new TwoOperationAlgorithm(source).CreateRun(problem, RandomNumberGenerator.Create(1), [mutator.Invocations.Counting(counter)]);
        run.Result(counter).CurrentCount.ShouldBe(0);

        run.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(4);
        run.Result(counter).CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void FluentCandidateMetricRejectsUnsupportedOperations()
    {
        var source = new CountingSource();
        Should.Throw<InvalidOperationException>(() => ReadSource(source).CountingCandidates(new Counter())).Message.ShouldContain("candidate work");
    }
}
