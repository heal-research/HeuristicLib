using HEAL.HeuristicLib.Encodings.Permutations;

namespace HEAL.HeuristicLib.Tests.Scenarios.Papers.AutoEC;

public partial class AutoEcPaperScenarioTests
{
    private static IReadOnlyList<AutoEcOperatorChoice<ICrossover<TCandidate>>> CreateCrossoverChoices<TCandidate>(
        IReadOnlyList<AutoEcOperatorChoice<ICrossover<TCandidate>>> choices) =>
    [
        .. choices,
        new(nameof(ChooseOneCrossover), ChooseOneCrossover.Create<TCandidate>(choices.Select(choice => choice.Operator).ToArray()))
    ];

    private static IReadOnlyList<AutoEcOperatorChoice<IMutator<TCandidate>>> CreateMutatorChoices<TCandidate>(
        IReadOnlyList<AutoEcOperatorChoice<IMutator<TCandidate>>> choices) =>
    [
        .. choices,
        new(nameof(ChooseOneMutator), ChooseOneMutator.Create<TCandidate>(choices.Select(choice => choice.Operator).ToArray()))
    ];

    private static IReadOnlyList<AutoEcOperatorChoice<ICrossover<Permutation>>> CreatePermutationCrossoverChoices() =>
        CreateCrossoverChoices<Permutation>([
            new(nameof(EdgeRecombinationCrossover), new EdgeRecombinationCrossover()),
            new(nameof(OrderCrossover), new OrderCrossover()),
            new(nameof(CosaCrossover), new CosaCrossover()),
            new(nameof(CyclicCrossover), new CyclicCrossover()),
            new(nameof(CyclicCrossover2), new CyclicCrossover2()),
            new(nameof(MaximalPreservativeCrossover), new MaximalPreservativeCrossover()),
            new(nameof(OrderBasedCrossover), new OrderBasedCrossover()),
            new(nameof(OrderCrossover2), new OrderCrossover2()),
            new(nameof(PartiallyMatchedCrossover), new PartiallyMatchedCrossover()),
            new(nameof(PositionBasedCrossover), new PositionBasedCrossover()),
            new(nameof(UniformLikeCrossover), new UniformLikeCrossover())
        ]);

    private static IReadOnlyList<AutoEcOperatorChoice<IMutator<Permutation>>> CreatePermutationMutatorChoices() =>
        CreateMutatorChoices<Permutation>([
            new(nameof(InversionMutator), new InversionMutator()),
            new(nameof(SwapMutator), new SwapMutator()),
            new(nameof(InsertionMutator), new InsertionMutator()),
            new(nameof(ScrambleMutator), new ScrambleMutator()),
            new(nameof(Swap3Mutator), new Swap3Mutator()),
            new(nameof(TranslocationMutator), new TranslocationMutator()),
            new(nameof(TranslocationInversionMutator), new TranslocationInversionMutator())
        ]);
}
