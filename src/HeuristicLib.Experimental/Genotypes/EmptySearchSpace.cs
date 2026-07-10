using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Genotypes;

public record EmptySearchSpace : ISearchSpace<EmptyGenotype>
{
    public static readonly EmptySearchSpace Instance = new();
    private EmptySearchSpace() { }
    public bool Contains(EmptyGenotype candidate) => true;
}
