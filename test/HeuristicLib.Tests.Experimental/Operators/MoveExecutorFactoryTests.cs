using HEAL.HeuristicLib.Operators.MoveAppliers;
using HEAL.HeuristicLib.Operators.MoveCreators;
using HEAL.HeuristicLib.Operators.MoveEvaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.MetaOptimization;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experimental.Operators;

public class MoveExecutorFactoryTests
{
    private static readonly UnrestrictedSearchSpace<int> SearchSpace = UnrestrictedSearchSpace<int>.Instance;
    private static readonly IProblem<int, UnrestrictedSearchSpace<int>> Problem = new NoProblem<int, UnrestrictedSearchSpace<int>>(SearchSpace);
    private static readonly IRandomNumberGenerator Random = RandomNumberGenerator.Create(1);

    [Theory]
    [InlineData("creator")]
    [InlineData("applier")]
    [InlineData("evaluator")]
    public void StatefulMoveRoles_PrepareOnceAcrossBindingsAndIsolateIndependentRoots(string role)
    {
        var preparations = new Counter();
        IOperator source = role switch
        {
            "creator" => new CountingCreator(preparations),
            "applier" => new CountingApplier(preparations),
            "evaluator" => new CountingEvaluator(preparations),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        var root = ResolutionScope.Create();
        Next(root).ShouldBe(1);
        Next(root.CreateChildScope()).ShouldBe(2);
        Next(root).ShouldBe(3);
        preparations.Value.ShouldBe(1);
        Next(ResolutionScope.Create()).ShouldBe(1);
        preparations.Value.ShouldBe(2);

        int Next(ResolutionScope scope) => source switch
        {
            IOperator<MoveCreation<int>, int> creator => scope.Resolve<int, UnrestrictedSearchSpace<int>, IProblem<int, UnrestrictedSearchSpace<int>>, int>(creator)
                .Moves(0, Random, SearchSpace, Problem).Single(),
            IOperator<MoveApplication<int>, int> applier => scope.Resolve<int, UnrestrictedSearchSpace<int>, IProblem<int, UnrestrictedSearchSpace<int>>, int>(applier)
                .Apply(0, 0, Random, SearchSpace, Problem),
            IOperator<MoveEvaluation<int>, int> evaluator => (int)scope.Resolve<int, UnrestrictedSearchSpace<int>, IProblem<int, UnrestrictedSearchSpace<int>>, int>(evaluator)
                .Evaluate(new ObjectiveVector(0), 0, 0, Random, SearchSpace, Problem)[0],
            _ => throw new InvalidOperationException()
        };
    }

    /// <summary>A move evaluation evaluates one candidate per call, so candidate counting counts its calls.</summary>
    [Fact]
    public void MoveEvaluation_ReportsOneCandidatePerCall()
    {
        IOperator<MoveEvaluation<int>, int> evaluator = new CountingEvaluator(new Counter());
        var candidates = new CountAccumulator();
        var scope = ResolutionScope.Create(Aspect.CountCandidates(evaluator, candidates));
        var executor = scope.Resolve<int, UnrestrictedSearchSpace<int>, IProblem<int, UnrestrictedSearchSpace<int>>, int>(evaluator);

        executor.Evaluate(new ObjectiveVector(0), 0, 0, Random, SearchSpace, Problem);
        executor.Evaluate(new ObjectiveVector(0), 0, 1, Random, SearchSpace, Problem);

        candidates.CurrentCount.ShouldBe(2);
    }

    private sealed class Counter
    {
        public int Value { get; set; }
    }

    private sealed record CountingCreator(Counter Preparations) : MoveCreator<int, UnrestrictedSearchSpace<int>, IProblem<int, UnrestrictedSearchSpace<int>>, int, Counter>
    {
        protected override Counter CreateInitialState()
        {
            Preparations.Value++;
            return new Counter();
        }

        protected override IEnumerable<int> Moves(int candidate, Counter state, IRandomNumberGenerator random, UnrestrictedSearchSpace<int> searchSpace, IProblem<int, UnrestrictedSearchSpace<int>> problem)
        {
            yield return ++state.Value;
        }
    }

    private sealed record CountingApplier(Counter Preparations) : MoveApplier<int, UnrestrictedSearchSpace<int>, IProblem<int, UnrestrictedSearchSpace<int>>, int, Counter>
    {
        protected override Counter CreateInitialState()
        {
            Preparations.Value++;
            return new Counter();
        }

        protected override int Apply(int candidate, int move, Counter state, IRandomNumberGenerator random, UnrestrictedSearchSpace<int> searchSpace, IProblem<int, UnrestrictedSearchSpace<int>> problem) => ++state.Value;
    }

    private sealed record CountingEvaluator(Counter Preparations) : MoveEvaluator<int, UnrestrictedSearchSpace<int>, IProblem<int, UnrestrictedSearchSpace<int>>, int, Counter>
    {
        protected override Counter CreateInitialState()
        {
            Preparations.Value++;
            return new Counter();
        }

        protected override ObjectiveVector Evaluate(ObjectiveVector oldQuality, int candidate, int move, Counter state, IRandomNumberGenerator random, UnrestrictedSearchSpace<int> searchSpace, IProblem<int, UnrestrictedSearchSpace<int>> problem) => new(++state.Value);
    }
}
