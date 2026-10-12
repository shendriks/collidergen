using System.Diagnostics.CodeAnalysis;

namespace ColliderGen;

public sealed record ColliderOptions
{
    public const int MinVerticesLimit = 3;
    public const int MaxVerticesLimit = 16;
    public byte AlphaThreshold { get; init; } = 128;
    public float Epsilon { get; init; }
    public int MaxVertices { get; init; } = MaxVerticesLimit;
    public ColliderOrigin Origin { get; init; } = ColliderOrigin.Center;
    public ColliderFit Fit { get; init; } = ColliderFit.Inner;
    public int? FrameWidth { get; init; }

    public bool TryValidate([NotNullWhen(false)] out string? error)
    {
        error = this switch {
            { MaxVertices: < MinVerticesLimit or > MaxVerticesLimit } => $"MaxVertices must be between {MinVerticesLimit} and {MaxVerticesLimit}.",
            { Epsilon: var epsilon } when !(epsilon >= 0f) => "Epsilon must be a non-negative number.",
            { FrameWidth: <= 0 } => "FrameWidth must be at least 1.",
            _ => null
        };
        return error is null;
    }
}
