namespace HEAL.HeuristicLib.Numerics;

/// <summary>
/// The working spans an operation asked for, each the same length as its result.
/// </summary>
/// <remarks>
/// An operation declares how many it needs through <see cref="IUnaryOperationDefinition.ScratchSpanCount"/> or
/// <see cref="IBinaryOperationDefinition.ScratchSpanCount"/>, and whoever runs it reserves that many.
/// </remarks>
internal readonly ref struct ScratchSpans
{
    private readonly Span<double> buffer;
    private readonly int spanLength;

    public ScratchSpans(Span<double> buffer, int spanLength)
    {
        if (spanLength < 0)
            throw new ArgumentOutOfRangeException(nameof(spanLength));

        this.buffer = buffer;
        this.spanLength = spanLength;
        Count = spanLength == 0 ? 0 : buffer.Length / spanLength;
    }

    public static ScratchSpans None => default;

    public int Count { get; }

    public Span<double> this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"The operation was given {Count} scratch spans.");

            return buffer.Slice(index * spanLength, spanLength);
        }
    }
}
