namespace HEAL.HeuristicLib.Genotypes;

public record EmptyGenotype
{
    public static readonly EmptyGenotype Instance = new();
    private EmptyGenotype() { }
}
