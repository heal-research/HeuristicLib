using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public sealed partial class OperatorPreparationSpecs
{
    [Fact]
    public void BudgetListsCountAllOperationsWhileSingleSelectionKeepsItsOperation()
    {
        var source = new CountingSource();
        var algorithm = new TwoOperationAlgorithm(source);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var combined = algorithm.LimitedToInvocations([source, source], 3);
        var single = algorithm.LimitedToInvocations((IOperator<Mutation, RealVector>)source, 3);
        var constructedSingle = new OperatorCountBudgetAlgorithm<Mutation, RealVector, SingleSolutionState<RealVector>>(
            algorithm, source, 3);
        var constructedCombined = new OperatorCountBudgetAlgorithm<RealVector, SingleSolutionState<RealVector>>(
            algorithm, [source, source], 3);

        combined.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        single.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
        constructedSingle.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
        constructedCombined.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        Should.Throw<InvalidOperationException>(() => algorithm.LimitedToCandidates([source], 3)
            .Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).ToList());
    }

    [Fact]
    public void DurationListsMeasureConsumerAndBuiltInOperationsWhileSingleSelectionStaysTyped()
    {
        var source = new CountingSource();
        var algorithm = new TwoOperationAlgorithm(source);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var clock = new BudgetClock();
        var combined = algorithm.LimitedToDuration([source, source], TimeSpan.FromTicks(3), clock);
        var single = algorithm.LimitedToDuration((IOperator<Mutation, RealVector>)source, TimeSpan.FromTicks(3), clock);
        var constructedSingle = new OperatorDurationBudgetAlgorithm<Mutation, RealVector, SingleSolutionState<RealVector>>(
            algorithm, source, TimeSpan.FromTicks(3))
        { TimeProvider = clock };
        var constructedCombined = new OperatorDurationBudgetAlgorithm<RealVector, SingleSolutionState<RealVector>>(
            algorithm, [source, source], TimeSpan.FromTicks(3))
        { TimeProvider = clock };

        combined.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        single.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
        constructedSingle.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
        constructedCombined.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
    }

    private sealed class BudgetClock : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp++;
    }

    private sealed record TwoOperationAlgorithm(CountingSource Source)
        : Algorithm<TwoOperationAlgorithm, RealVector, SingleSolutionState<RealVector>>
    {
        protected override IAlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope)
        {
            var reader = (IReadValueExecutor<RealVector, TSearchSpace, TProblem>)scope.ResolveOperator((IOperator<ReadValue, RealVector>)Source);
            var mutator = scope.Resolve((IOperator<Mutation, RealVector>)Source);
            return new Executor<TSearchSpace, TProblem>(reader, mutator);
        }

        private sealed class Executor<TSearchSpace, TProblem>(
            IReadValueExecutor<RealVector, TSearchSpace, TProblem> reader,
            IMutationExecutor<RealVector, TSearchSpace, TProblem> mutator)
            : AlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override async IAsyncEnumerable<SingleSolutionState<RealVector>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
                SingleSolutionState<RealVector>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var i = 0; i < 4; i++)
                {
                    var value = reader.Read(random, problem.SearchSpace, problem);
                    var candidate = mutator.Mutate([new RealVector(value)], random, problem.SearchSpace, problem)[0];
                    yield return SingleSolutionState.From(candidate, new ObjectiveVector(0));
                }
                await Task.CompletedTask;
            }
        }
    }
}
