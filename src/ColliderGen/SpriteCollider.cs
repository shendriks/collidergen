using System.Numerics;

namespace ColliderGen;

public sealed record SpriteCollider(int FrameWidth, Vector2 Origin, IReadOnlyList<IReadOnlyList<Vector2>> Frames)
{
    public int FrameCount => Frames.Count;
}
