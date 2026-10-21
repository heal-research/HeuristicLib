using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators;

public sealed class TypedRoleAdviceTests
{
    [Theory]
    [InlineData("creation")]
    [InlineData("crossover")]
    [InlineData("selection")]
    [InlineData("refinement")]
    [InlineData("replacement")]
    public void TypedAdvice_ChecksBeforeConstructionAndRetriesWithoutResettingTheOwner(string role)
    {
        var source = new SingleResultOperator();
        var advice = new AdviceState { Reject = true };
        var root = ResolutionScope.Create(ForRole(role, source, advice));

        Should.Throw<InvalidOperationException>(() => Invoke(role, root, source)).Message.ShouldBe("rejected");
        source.Constructions.ShouldBe(0);
        advice.Wraps.ShouldBe(0);

        advice.Reject = false;
        Invoke(role, root, source).ShouldBe(1);
        Invoke(role, root.CreateChildScope(), source).ShouldBe(2);
        Invoke(role, root, source).ShouldBe(3);
        source.Preparations.ShouldBe(1);
        source.Constructions.ShouldBe(3);
        advice.Checks.ShouldBe(4);
        advice.Wraps.ShouldBe(3);

        // A different operation uses the same prepared owner and state; the typed advice does not apply to it.
        Invoke(role == "creation" ? "replacement" : "creation", root, source).ShouldBe(4);
        advice.Checks.ShouldBe(4);
        source.Preparations.ShouldBe(1);
    }

    private static readonly DummySearchSpace<int> Space = DummySearchSpace<int>.Instance;
    private static readonly IProblem<int, DummySearchSpace<int>> Problem = FuncProblem.Create((int x) => x, Space, SingleObjective.Minimize);

    private static int Invoke(string role, ResolutionScope root, SingleResultOperator source)
    {
        var scope = root.For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>();
        var random = new SequenceRandomNumberGenerator();
        EvaluatedCandidate<int>[] population = [new(0, new ObjectiveVector(0))];
        return role switch
        {
            "creation" => scope.Resolve(((IOperator<Creation, int>)source).Retry(2).Named("creator")).Create(1, random, Space, Problem)[0],
            "crossover" => scope.Resolve(((IOperator<Crossover, int>)source).Retry(2).Named("crossover")).Cross([Parents.From(0, 1)], random, Space, Problem)[0],
            "selection" => scope.Resolve(((IOperator<Selection, int>)source).Retry(2).Named("selector")).Select(population, Problem.Objective, 1, random, Space, Problem)[0].Candidate,
            "refinement" => scope.Resolve(((IOperator<Refinement, int>)source).Retry(2).Named("refiner")).Refine([0], random, Space, Problem)[0],
            "replacement" => scope.Resolve(((IOperator<Replacement, int>)source).Retry(2).Named("replacer")).Replace(population, population, Problem.Objective, 1, random, Space, Problem)[0].Candidate,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }

    private sealed class AdviceState
    {
        public bool Reject { get; set; }
        public int Checks { get; private set; }
        public int Wraps { get; set; }
        public void Check()
        {
            Checks++;
            if (Reject)
                throw new InvalidOperationException("rejected");
        }
    }

    private static Aspect ForRole(string role, SingleResultOperator source, AdviceState state) => role switch
    {
        "creation" => Aspect.Create(Pointcut.Reference<IOperator<Creation, int>>(source), new CreationAdvice(state)),
        "crossover" => Aspect.Create(Pointcut.Reference<IOperator<Crossover, int>>(source), new CrossAdvice(state)),
        "selection" => Aspect.Create(Pointcut.Reference<IOperator<Selection, int>>(source), new SelectionAdvice(state)),
        "refinement" => Aspect.Create(Pointcut.Reference<IOperator<Refinement, int>>(source), new RefinementAdvice(state)),
        "replacement" => Aspect.Create(Pointcut.Reference<IOperator<Replacement, int>>(source), new ReplacementAdvice(state)),
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    private sealed class CreationAdvice(AdviceState state) : CreatorAdvice<int>
    {
        protected override void Check<TSearchSpace, TProblem>(IOperator<Creation, int> creator) => state.Check();
        protected override ICreationExecutor<int, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(ICreationExecutor<int, TSearchSpace, TProblem> executor, IOperator<Creation, int> creator)
        {
            state.Wraps++;
            return executor;
        }
    }
    private sealed class CrossAdvice(AdviceState state) : CrossoverAdvice<int>
    {
        protected override void Check<TSearchSpace, TProblem>(IOperator<Crossover, int> crossover) => state.Check();
        protected override ICrossoverExecutor<int, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(ICrossoverExecutor<int, TSearchSpace, TProblem> executor, IOperator<Crossover, int> crossover)
        {
            state.Wraps++;
            return executor;
        }
    }
    private sealed class SelectionAdvice(AdviceState state) : SelectorAdvice<int>
    {
        protected override void Check<TSearchSpace, TProblem>(IOperator<Selection, int> selector) => state.Check();
        protected override ISelectionExecutor<int, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(ISelectionExecutor<int, TSearchSpace, TProblem> executor, IOperator<Selection, int> selector)
        {
            state.Wraps++;
            return executor;
        }
    }
    private sealed class RefinementAdvice(AdviceState state) : RefinerAdvice<int>
    {
        protected override void Check<TSearchSpace, TProblem>(IOperator<Refinement, int> refiner) => state.Check();
        protected override IRefinementExecutor<int, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(IRefinementExecutor<int, TSearchSpace, TProblem> executor, IOperator<Refinement, int> refiner)
        {
            state.Wraps++;
            return executor;
        }
    }
    private sealed class ReplacementAdvice(AdviceState state) : ReplacerAdvice<int>
    {
        protected override void Check<TSearchSpace, TProblem>(IOperator<Replacement, int> replacer) => state.Check();
        protected override IReplacementExecutor<int, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(IReplacementExecutor<int, TSearchSpace, TProblem> executor, IOperator<Replacement, int> replacer)
        {
            state.Wraps++;
            return executor;
        }
    }
}
