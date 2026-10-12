namespace ColliderGen;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class InnerFitStep : ISimplificationStep
{
    /// <summary>
    /// Calculates the cost of removing a vertex from the hull based on the area of the triangle
    /// formed by the vertex and its two neighbors.
    /// </summary>
    public static float Cost(HullVertex vertex)
    {
        var a = vertex.Prev.Point;
        var b = vertex.Point;
        var c = vertex.Next.Point;
        return MathF.Abs((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y)) * 0.5f;
    }

    public static bool TryApply(HullVertex vertex, out IReadOnlyList<HullVertex> affected)
    {
        var (prev, next) = vertex.Unlink();
        affected = [prev, next];
        return true;
    }
}
