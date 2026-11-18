using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems.TestFunctions;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public sealed class TypedResolutionScopeTests
{
    private static readonly TestFunctionProblem Problem = new(new SphereFunction(1));

    [Fact]
    public void AFourArgumentScope_HeldAsAThreeArgumentScope_ChecksItsOwnSearchState()
    {
        var source = new SingleSolutionMutator();
        var root = ResolutionScope.Create();
        var withoutState = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Problem);
        withoutState.TryResolve(source, out var rejected, out var rejection).ShouldBeFalse();
        rejected.ShouldBeNull();
        rejection.ShouldNotBeNull();

        ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> scope =
            root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(Problem);

        scope.TryResolve(source, out var executor, out var reason).ShouldBeTrue();
        executor.ShouldBeSameAs(source);
        reason.ShouldBeNull();
        scope.Resolve(source).ShouldBeSameAs(executor);
        scope.ResolveOptional(source).ShouldBeSameAs(executor);
        scope.ResolveOperator(source).ShouldBeSameAs(executor);
        scope.TryResolveOperator(source, out IMutationExecutor<RealVector>? genericExecutor, out var genericReason).ShouldBeTrue();
        genericExecutor.ShouldBeSameAs(executor);
        genericReason.ShouldBeNull();
    }

    [Fact]
    public void AFourArgumentScope_ResolvesAllSevenCandidateRoles()
    {
        var scope = ResolutionScope.Create()
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(Problem);
        var creator = new UniformDistributedCreator();
        var crossover = new SelectFirstParentCrossover<RealVector>();
        var evaluator = new ProblemEvaluator<RealVector>();
        var mutator = new SingleSolutionMutator();
        var refiner = new NoChangeRefiner<RealVector>();
        var replacer = new PlusSelectionReplacer<RealVector>();
        var selector = new BestSelector<RealVector>();

        var creation = scope.Resolve(creator);
        scope.ResolveOptional(creator).ShouldBeSameAs(creation);
        scope.TryResolve(creator, out var triedCreation, out var creationReason).ShouldBeTrue();
        triedCreation.ShouldBeSameAs(creation);
        creationReason.ShouldBeNull();

        var crossing = scope.Resolve(crossover);
        scope.ResolveOptional(crossover).ShouldBeSameAs(crossing);
        scope.TryResolve(crossover, out var triedCrossover, out var crossoverReason).ShouldBeTrue();
        triedCrossover.ShouldBeSameAs(crossing);
        crossoverReason.ShouldBeNull();

        var evaluation = scope.Resolve(evaluator);
        scope.ResolveOptional(evaluator).ShouldBeSameAs(evaluation);
        scope.TryResolve(evaluator, out var triedEvaluation, out var evaluationReason).ShouldBeTrue();
        triedEvaluation.ShouldBeSameAs(evaluation);
        evaluationReason.ShouldBeNull();

        var mutation = scope.Resolve(mutator);
        scope.ResolveOptional(mutator).ShouldBeSameAs(mutation);
        scope.TryResolve(mutator, out var triedMutation, out var mutationReason).ShouldBeTrue();
        triedMutation.ShouldBeSameAs(mutation);
        mutationReason.ShouldBeNull();

        var refinement = scope.Resolve(refiner);
        scope.ResolveOptional(refiner).ShouldBeSameAs(refinement);
        scope.TryResolve(refiner, out var triedRefinement, out var refinementReason).ShouldBeTrue();
        triedRefinement.ShouldBeSameAs(refinement);
        refinementReason.ShouldBeNull();

        var replacement = scope.Resolve(replacer);
        scope.ResolveOptional(replacer).ShouldBeSameAs(replacement);
        scope.TryResolve(replacer, out var triedReplacement, out var replacementReason).ShouldBeTrue();
        triedReplacement.ShouldBeSameAs(replacement);
        replacementReason.ShouldBeNull();

        var selection = scope.Resolve(selector);
        scope.ResolveOptional(selector).ShouldBeSameAs(selection);
        scope.TryResolve(selector, out var triedSelection, out var selectionReason).ShouldBeTrue();
        triedSelection.ShouldBeSameAs(selection);
        selectionReason.ShouldBeNull();
    }

    private sealed record SingleSolutionMutator : StatelessMutator<RealVector>
    {
        public override bool Supports(ExecutionSignature execution) =>
            execution.SearchState == typeof(SingleSolutionState<RealVector>);

        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) => parents;
    }
}
