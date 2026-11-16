using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public sealed partial class OperatorPreparationSpecs
{
    // The general form counts each operation it places a wrapper on, so counting both operations of one configuration
    // takes one Counting per operation. The single-operator form counts the operation its argument is typed for.
    [Fact]
    public void GeneralCountBudgetCountsEachPlacedOperationWhileSingleSelectionKeepsItsOperation()
    {
        var source = new CountingSource();
        var algorithm = new TwoOperationAlgorithm(source);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var combined = algorithm.LimitedToCount(3, (a, calls) => a.Counting(a.Reader, calls).Counting(a.Mutator, calls));
        var single = algorithm.LimitedToInvocations((IOperator<Mutation, RealVector>)source, 3);
        var constructed = new OperatorCountBudgetAlgorithm<RealVector, SingleSolutionState<RealVector>>(
            algorithm.Counting(algorithm.Mutator, out var calls), calls, 3);

        combined.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        single.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
        constructed.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
        Should.Throw<InvalidOperationException>(() => algorithm.LimitedToCount(3, (a, candidates) => a.Counting(a.Reader, candidates, OperatorCountMetric.Candidates))
            .Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).ToList());
    }

    [Fact]
    public void GeneralDurationBudgetMeasuresConsumerAndBuiltInOperationsWhileSingleSelectionStaysTyped()
    {
        var source = new CountingSource();
        var algorithm = new TwoOperationAlgorithm(source);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var clock = new BudgetClock();
        var combined = algorithm.LimitedToDuration(TimeSpan.FromTicks(3), (a, time) => a.Timing(a.Reader, time).Timing(a.Mutator, time), clock);
        var single = algorithm.LimitedToDuration((IOperator<Mutation, RealVector>)source, TimeSpan.FromTicks(3), clock);
        var constructed = new OperatorDurationBudgetAlgorithm<RealVector, SingleSolutionState<RealVector>>(
            algorithm.Timing(algorithm.Mutator, out var time, clock), time, TimeSpan.FromTicks(3));

        combined.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        single.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
        constructed.Stream(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
    }

    private sealed class BudgetClock : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp++;
    }

    // One slot per operation, so a wrapper of one operation fits its slot while the other keeps the configuration.
    private sealed record TwoOperationAlgorithm(IOperator<ReadValue, RealVector> Reader, IOperator<Mutation, RealVector> Mutator)
        : Algorithm<TwoOperationAlgorithm, RealVector, SingleSolutionState<RealVector>>
    {
        public TwoOperationAlgorithm(CountingSource source)
            : this((IOperator<ReadValue, RealVector>)source, (IOperator<Mutation, RealVector>)source)
        { }

        public static Parameter<TwoOperationAlgorithm, IOperator<ReadValue, RealVector>> ReaderParameter { get; } =
            new(nameof(Reader), static algorithm => algorithm.Reader, static (algorithm, value) => algorithm with { Reader = value });

        public static Parameter<TwoOperationAlgorithm, IOperator<Mutation, RealVector>> MutatorParameter { get; } =
            new(nameof(Mutator), static algorithm => algorithm.Mutator, static (algorithm, value) => algorithm with { Mutator = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), ReaderParameter, MutatorParameter];

        protected override IAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope)
        {
            var reader = (IReadValueExecutor)scope.ResolveOperator(Reader);
            var mutator = scope.Resolve(Mutator);
            return new Executor(reader, mutator);
        }

        private sealed class Executor(
            IReadValueExecutor reader,
            IMutationExecutor<RealVector> mutator)
            : AlgorithmExecutor<SingleSolutionState<RealVector>>
        {
            public override async IAsyncEnumerable<SingleSolutionState<RealVector>> RunStreamingAsync(IRandomNumberGenerator random, SingleSolutionState<RealVector>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var i = 0; i < 4; i++)
                {
                    var value = reader.Read(random);
                    var candidate = mutator.Mutate([new RealVector(value)], random)[0];
                    yield return SingleSolutionState.From(candidate, new ObjectiveVector(0));
                }
                await Task.CompletedTask;
            }
        }
    }
}
