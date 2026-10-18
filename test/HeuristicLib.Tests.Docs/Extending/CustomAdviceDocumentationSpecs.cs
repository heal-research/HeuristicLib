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
public sealed class MutationLogAdvice<TCandidate> : OperatorAdvice<Mutation, TCandidate>
{
    private readonly Action<IOperator<Mutation, TCandidate>, int, int> write;

    public MutationLogAdvice(Action<IOperator<Mutation, TCandidate>, int, int> write)
    {
        this.write = write;
    }

    public override OperatorAdviceWrapperFactory<Mutation, TCandidate> CreateWrapperFactory(
        IOperator<Mutation, TCandidate> mutator) => new Factory(mutator, write);

    private sealed class Factory(
        IOperator<Mutation, TCandidate> mutator,
        Action<IOperator<Mutation, TCandidate>, int, int> write)
        : OperatorAdviceWrapperFactory<Mutation, TCandidate>, IInvocationWrapper
    {
        private int calls;

        public override WrapperExecutorFactory GetWrapperFactory<TSearchSpace, TProblem, TSearchState>(
            IOperator<Mutation, TCandidate> source,
            OperationExecutorAdapter<Mutation, TCandidate, TSearchSpace, TProblem, TSearchState> adapter) =>
            adapter.WrapInvocation(this);

        public Func<TArguments, TResult> Wrap<TArguments, TResult>(Func<TArguments, TResult> invokeChild,
            Func<TArguments, TResult, int>? countCandidateWork)
        {
            var count = countCandidateWork ?? throw new InvalidOperationException("Mutation must supply its candidate count.");
            return arguments =>
            {
                var result = invokeChild(arguments);
                Record(count(arguments, result));
                return result;
            };
        }

        private void Record(int candidateWork)
        {
            calls++;
            write(mutator, calls, candidateWork);
        }

    }
}
#endregion custom-advice-type

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
    public void RebindingKeepsJoinPointStateWhileIndependentRootsStartFresh()
    {
        var mutator = new NoChangeMutator<RealVector>();
        var calls = new List<int>();
        var advice = new MutationLogAdvice<RealVector>((_, call, _) => calls.Add(call));
        var logging = Aspect.Create(Pointcut.Reference(mutator), advice);
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        var random = RandomNumberGenerator.Create(seed: 42);
        IReadOnlyList<RealVector> parents = [new RealVector(0.0)];
        var scope = ResolutionScope.Create(logging);
        var original = scope.For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>().Resolve(mutator);
        var rebound = scope.With().For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>().Resolve(mutator);

        original.Mutate(parents, random, searchSpace, problem);
        rebound.Mutate(parents, random, searchSpace, problem);
        var independent = ResolutionScope.Create(logging)
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>().Resolve(mutator);
        independent.Mutate(parents, random, searchSpace, problem);

        calls.ShouldBe([1, 2, 1]);
    }
}
