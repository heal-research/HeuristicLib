using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record CancellationTokenTerminator<TCandidate>(CancellationToken CancellationToken)
    : StatelessTerminator<TCandidate>
{
    public static Parameter<CancellationTokenTerminator<TCandidate>, CancellationToken> CancellationTokenParameter { get; } =
        new(nameof(CancellationToken), static terminator => terminator.CancellationToken, static (terminator, value) => terminator with { CancellationToken = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [CancellationTokenParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public override bool IsTerminalState() => CancellationToken.IsCancellationRequested;
}

public static class CancellationTokenTerminator
{
    public static CancellationTokenTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, CancellationToken cancellationToken) => new(cancellationToken);
}
