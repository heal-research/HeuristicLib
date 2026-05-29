namespace HEAL.HeuristicLib.Problems.MetaOptimization;

public record EmptyGenotype
{
    public static readonly EmptyGenotype Instance = new();
    private EmptyGenotype() { }
}
