namespace ColliderGen.Cli;

internal sealed record ColliderDto(FrameDto[] Frames)
{
    internal static ColliderDto From(SpriteCollider collider) =>
        new([.. collider.Frames.Select(frame => new FrameDto([.. frame.Select(vertex => new VertexDto(vertex.X, vertex.Y))]))]);
}

internal sealed record FrameDto(VertexDto[] Vertices);

internal sealed record VertexDto(float X, float Y);
