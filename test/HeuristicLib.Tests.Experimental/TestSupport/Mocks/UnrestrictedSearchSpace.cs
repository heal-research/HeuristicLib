using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

internal sealed class UnrestrictedSearchSpace<TCandidate> : ISearchSpace<TCandidate>
{
    public static UnrestrictedSearchSpace<TCandidate> Instance { get; } = new();
    public bool Contains(TCandidate candidate) => true;
}
