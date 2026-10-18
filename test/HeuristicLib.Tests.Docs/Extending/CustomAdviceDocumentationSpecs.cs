#region custom-advice-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
#endregion custom-advice-imports

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

#region custom-advice-type
public sealed class MutationLogAdvice<TCandidate>(Action<IOperator<Mutation, TCandidate>, int, int> write)
    : MutatorAdvice<TCandidate>
{
    private int calls;

    protected override IMutationExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        IMutationExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Mutation, TCandidate> mutator) =>
        new LoggingExecutor<TSearchSpace, TProblem>(executor, mutator, this);

    private void Record(IOperator<Mutation, TCandidate> mutator, int offspring) => write(mutator, ++calls, offspring);

    private sealed class LoggingExecutor<TSearchSpace, TProblem>(
        IMutationExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IOperator<Mutation, TCandidate> mutator,
        MutationLogAdvice<TCandidate> advice)
        : IMutationExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
            TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = executor.Mutate(parents, random, searchSpace, problem);
            advice.Record(mutator, offspring.Count);
            return offspring;
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
    }
}
#endregion custom-advice-type

#region general-advice-type
public sealed class SlowCallAdvice(TimeSpan threshold, TimeProvider time, Action<IOperator, TimeSpan> report) : OperatorAdvice
{
    public override IExecutor Wrap(IExecutor executor, OperationExecutorAdapter operation, IOperator source) =>
        operation.WrapInvocation(executor, new ReportSlowCalls(threshold, time, report, source));

    private readonly struct ReportSlowCalls(TimeSpan threshold, TimeProvider time, Action<IOperator, TimeSpan> report,
        IOperator source) : IInvocationWrapper
    {
        public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
        {
            var start = time.GetTimestamp();
            var result = invocation.Proceed();
            var elapsed = time.GetElapsedTime(start);
            if (elapsed >= threshold)
                report(source, elapsed);
            return result;
        }
    }
}
#endregion general-advice-type

public sealed class CustomAdviceDocumentationSpecs
{
    [Fact]
    public async Task AdviceLogsTheSelectedMutatorInAPreparedRun()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        #region custom-advice-run
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create(
            (RealVector candidate) => candidate[0] * candidate[0],
            searchSpace,
            SingleObjective.Minimize);
        var mutator = new NoChangeMutator<RealVector>();
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = mutator,
            PopulationSize = 4,
            MutationRate = 1.0,
            MaximumGenerations = 3
        };
        var messages = new List<string>();
        var advice = new MutationLogAdvice<RealVector>((source, call, count) =>
            messages.Add($"{source.GetType().Name}: call {call}, {count} offspring"));
        var logging = Aspect.Create(Pointcut.Reference(mutator), advice);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .AddAspect(logging);
        await run.CompleteAsync(cancellationToken: cancellationToken);
        #endregion custom-advice-run

        messages.Count.ShouldBe(2);
        messages[0].ShouldBe("NoChangeMutator`1: call 1, 4 offspring");
        messages[1].ShouldBe("NoChangeMutator`1: call 2, 4 offspring");
    }

    [Fact]
    public void TheAdviceObjectOwnsItsCallCounterAcrossRebindingAndRoots()
    {
        var mutator = new NoChangeMutator<RealVector>();
        var calls = new List<int>();
        var advice = new MutationLogAdvice<RealVector>((_, call, _) => calls.Add(call));
        var logging = Aspect.Create(Pointcut.Reference(mutator), advice);
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        var random = RandomNumberGenerator.Create(42);
        IReadOnlyList<RealVector> parents = [new RealVector(0.0)];
        var scope = ResolutionScope.Create(logging);
        var original = scope.For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>().Resolve(mutator);
        var rebound = scope.With().For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>().Resolve(mutator);

        original.Mutate(parents, random, searchSpace, problem);
        rebound.Mutate(parents, random, searchSpace, problem);
        var independent = ResolutionScope.Create(logging)
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>().Resolve(mutator);
        independent.Mutate(parents, random, searchSpace, problem);

        calls.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task GeneralAdviceWrapsEveryOperationOfTheSelectedOperators()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new NoChangeMutator<RealVector>(),
            PopulationSize = 4,
            MutationRate = 1.0,
            MaximumGenerations = 3
        };

        #region general-advice-run
        var slow = new List<string>();
        var advice = new SlowCallAdvice(TimeSpan.Zero, TimeProvider.System,
            (source, elapsed) => slow.Add(source.GetType().Name));
        var operators = Pointcut.Reference<IOperator>(algorithm.Mutator) | Pointcut.Reference<IOperator>(algorithm.Evaluator);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .AddAspect(Aspect.Create(operators, advice));
        #endregion general-advice-run
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        slow.ShouldContain(algorithm.Mutator.GetType().Name);
        slow.ShouldContain(algorithm.Evaluator.GetType().Name);
    }
}
