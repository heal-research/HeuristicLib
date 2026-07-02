using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.TestSupport.Mocks;

public sealed class DummySearchSpace<TCandidate> : ISearchSpace<TCandidate>
{
    public static readonly DummySearchSpace<TCandidate> Instance = new();
    private DummySearchSpace() { }
    public bool Contains(TCandidate candidate) => true;
}
