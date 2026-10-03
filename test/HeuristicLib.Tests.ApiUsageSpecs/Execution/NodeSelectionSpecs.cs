using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems.TestFunctions;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public class NodeSelectionSpecs
{
    [Fact]
    public void AReferenceRetainsItsDeclaredRole()
    {
        IMutator<RealVector> mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        NodeSelector<IMutator<RealVector>> selected = NodeSelector.Reference(mutator);

        selected.Matches(mutator).ShouldBeTrue();
        selected.Matches(new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5)).ShouldBeFalse();
    }

    [Fact]
    public void RoleAndConcreteTypeSelectionsSupportConfigurationFilters()
    {
        var smallMutation = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        var largeMutation = smallMutation with { MutationStrength = 2.0 };

        NodeSelector<IMutator<RealVector>> selectedMutators = NodeSelector.OfType<IMutator<RealVector>>()
            .And(mutator => mutator is GaussianMutator { MutationStrength: < 1.0 });
        NodeSelector<GaussianMutator> selectedGaussianMutators = NodeSelector.OfType<GaussianMutator>()
            .And(mutator => mutator.MutationStrength < 1.0);

        selectedMutators.Matches(smallMutation).ShouldBeTrue();
        selectedGaussianMutators.Matches(smallMutation).ShouldBeTrue();
        selectedMutators.Matches(largeMutation).ShouldBeFalse();
        selectedGaussianMutators.Matches(largeMutation).ShouldBeFalse();
    }

    [Fact]
    public void NamesCanBePropertiesOnConsumerAuthoredWrappers()
    {
        var child = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        var exploration = new NamedMutator<RealVector>(child, "exploration");
        var refinement = new NamedMutator<RealVector>(child, "refinement");
        var allMutators = NodeSelector.OfType<IMutator<RealVector>>();
        NodeSelector<IMutator<RealVector>> selected = allMutators
            .And(mutator => mutator is NamedMutator<RealVector> { Name: "exploration" });

        // These are explicitly supplied nodes; resolver integration is a later package.
        IConfigurationNode[] nodes = [child, exploration, refinement];
        nodes.Where(selected.Matches).ShouldBe([exploration]);
        nodes.Where(allMutators.Matches).ShouldBe(nodes);
    }

    [Fact]
    public void NamingWrappersCanRetainTheirSharedChildExecution()
    {
        var child = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        var exploration = new NamedMutator<RealVector>(child, "exploration");
        var refinement = new NamedMutator<RealVector>(child, "refinement");
        var scope = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();

        var explorationExecution = scope.Resolve(exploration);
        var refinementExecution = scope.Resolve(refinement);

        explorationExecution.ShouldBeSameAs(refinementExecution);
        scope.Resolve(child).ShouldBeSameAs(explorationExecution);
    }

    [Fact]
    public void ReferenceAndRoleSelectionsComposeWithoutLosingTheRole()
    {
        IMutator<RealVector> preferred = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        IMutator<RealVector> alternative = new GaussianMutator(mutationRate: 0.2, mutationStrength: 2.0);
        var exactReference = NodeSelector.Reference(preferred);
        var smallMutations = NodeSelector.OfType<IMutator<RealVector>>()
            .And(mutator => mutator is GaussianMutator { MutationStrength: < 1.0 });

        NodeSelector<IMutator<RealVector>> selected = exactReference.Or(smallMutations);
        NodeSelector<IMutator<RealVector>> sameSelection = exactReference | smallMutations;
        IConfigurationNode[] nodes = [preferred, alternative];

        nodes.Where(selected.Matches).ShouldBe([preferred]);
        nodes.Where(sameSelection.Matches).ShouldBe([preferred]);
    }

    [Fact]
    public void APropertyConditionCanBeReusedAsASelector()
    {
        var child = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.5);
        IMutator<RealVector> exploration = new NamedMutator<RealVector>(child, "exploration");
        IMutator<RealVector> refinement = new NamedMutator<RealVector>(child, "refinement");
        var namedExploration = new NodeSelector<IMutator<RealVector>>(
            mutator => mutator is NamedMutator<RealVector> { Name: "exploration" });

        var namedReferences = NodeSelector.Reference(exploration) | NodeSelector.Reference(refinement);
        NodeSelector<IMutator<RealVector>> selected = namedReferences & namedExploration;
        var sameSelection = namedReferences.And(
            mutator => mutator is NamedMutator<RealVector> { Name: "exploration" });
        IConfigurationNode[] nodes = [child, exploration, refinement];

        nodes.Where(selected.Matches).ShouldBe([exploration]);
        nodes.Where(sameSelection.Matches).ShouldBe([exploration]);
    }

    // This consumer-authored example is not a new library naming contract.
    private sealed record NamedMutator<TCandidate> : WrappingMutator<TCandidate>
    {
        public NamedMutator(IMutator<TCandidate> childMutator, string name)
            : base(childMutator)
        {
            Name = name;
        }

        public string Name { get; init; }

        protected override WrapperExecutionFactory<IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() => childMutator => childMutator;
    }
}
