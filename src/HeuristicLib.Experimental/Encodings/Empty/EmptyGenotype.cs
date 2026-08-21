namespace HEAL.HeuristicLib.Encodings.Empty;

public record EmptyGenotype
{
    public static readonly EmptyGenotype Instance = new();
    private EmptyGenotype() { }
}
