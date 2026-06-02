using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public sealed record RoarNetSearchSpace : ISearchSpace<Solution>
{
    public bool Contains(Solution genotype) => true;
}
