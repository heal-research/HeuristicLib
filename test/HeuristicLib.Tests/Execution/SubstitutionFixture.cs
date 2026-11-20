using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Execution;

// The fixture notation: A and B call the shared configuration M and yield states, and a recording shows what ran.
internal static class SubstitutionFixture
{
    internal static IProblem<int, DummySearchSpace<int>> Problem { get; } = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    internal static IRandomNumberGenerator Rng => RandomNumberGenerator.Create(1);

    internal static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal static void Run(IAlgorithm<int, PopulationState<int>> algorithm) => algorithm.CreateRun(Problem, Rng).Complete(cancellationToken: Ct);

    internal static CycleAlgorithm<int, PopulationState<int>> Cycle(
        IAlgorithm<int, PopulationState<int>> first, IAlgorithm<int, PopulationState<int>> second) =>
        CycleAlgorithm.Create(first, second) with { MaximumCycles = 2 };

    internal static (Recording, IOperator<Evaluation, int>) NewMutator()
    {
        var box = new Recording();
        return (box, new M(box));
    }

    internal static Caller NewCaller(string label, int callsPerState, int states, IOperator<Evaluation, int> mutator, Recording box) =>
        new(label, callsPerState, states, mutator, box);

    internal sealed class Recording
    {
        public int Calls;
        public int Builds;
        public readonly List<string> Entries = [];
        public string Log => string.Join(" ", Entries);
        public string Yields => string.Join(" ", Entries.Where(entry => !entry.StartsWith("build", StringComparison.Ordinal)));

        public void Reset()
        {
            Calls = Builds = 0;
            Entries.Clear();
        }
    }

    // Stands in for the mutator M, bound to its search space and problem like a problem-specific operator.
    internal sealed record M(Recording Box) : Evaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public bool FailFirstCall { get; init; }

        protected override IEvaluationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TRunSearchState> scope)
        {
            Box.Builds++;
            return new Executor(Box, FailFirstCall, scope.Problem);
        }

        private sealed class Executor(Recording box, bool failFirstCall, IProblem<int, DummySearchSpace<int>> problem) : EvaluationExecutor<int>
        {
            private bool failed = !failFirstCall;

            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
            {
                box.Calls++;
                if (!failed)
                {
                    failed = true;
                    throw new RetryableOperationException("The first call fails.");
                }
                return problem.Evaluate(candidates, random).ToArray();
            }
        }
    }

    // Produces up to States states. Each state costs CallsPerState calls to Mutator with the candidate equal to the state
    // number, plus the same calls to Second when it is set.
    internal sealed record Caller(string Label, int CallsPerState, int States, IOperator<Evaluation, int> Mutator, Recording Box)
        : Algorithm<Caller, int, PopulationState<int>>
    {
        public IOperator<Evaluation, int>? Second { get; init; }

        public static Parameter<Caller, IOperator<Evaluation, int>> MutatorParameter { get; } =
            new(nameof(Mutator), static caller => caller.Mutator, static (caller, value) => caller with { Mutator = value });

        public static Parameter<Caller, IOperator<Evaluation, int>?> SecondParameter { get; } =
            new(nameof(Second), static caller => caller.Second, static (caller, value) => caller with { Second = value });

        public static IReadOnlyList<IParameter> Parameters { get; } = [MutatorParameter, SecondParameter];

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
        {
            Box.Entries.Add($"build {Label}");
            return new Executor(this, scope.Resolve(Mutator), Second is null ? null : scope.Resolve(Second));
        }

        private sealed class Executor(Caller caller, IEvaluationExecutor<int> mutator,
            IEvaluationExecutor<int>? second)
            : AlgorithmExecutor<PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var state = 1; state <= caller.States; state++)
                {
                    ObjectiveVector quality = default!;
                    for (var call = 0; call < caller.CallsPerState; call++)
                    {
                        quality = mutator.Evaluate([state], random).Single();
                        second?.Evaluate([state], random);
                    }
                    caller.Box.Entries.Add($"{caller.Label}{state}");
                    yield return Population.From([state.ToEvaluated(quality)]).ToPopulationState();
                }
                await Task.CompletedTask;
            }
        }
    }
}
