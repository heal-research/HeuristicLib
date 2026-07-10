using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public sealed record RoarNetOperationsSearchSpace : ISearchSpace<Solution>
{
    public static RoarNetOperationsSearchSpace Instance { get; } = new();
    private RoarNetOperationsSearchSpace() { }
    public bool Contains(Solution genotype) => true;
}
