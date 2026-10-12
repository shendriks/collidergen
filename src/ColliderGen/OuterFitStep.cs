using System.Numerics;

namespace ColliderGen;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class OuterFitStep : ISimplificationStep
{
    /// <summary>
    /// Returns the area added by removing the edge from <paramref name="edgeStart"/> to its successor, i.e. the triangle formed by the edge
    /// and the point where its extended neighboring edges meet.
    /// </summary>
    /// <remarks>
    /// Returns <see cref="float.PositiveInfinity"/> when the neighbors are parallel or meet on the inner side, which makes the edge
    /// unremovable.
    /// </remarks>
    public static float Cost(HullVertex edgeStart)
    {
        if (!TryExtendNeighborsToIntersection(edgeStart, out var intersection)) {
            return float.PositiveInfinity;
        }

        var edge = edgeStart.Next.Point - edgeStart.Point;
        return MathF.Abs(Cross(edge, intersection - edgeStart.Point)) * 0.5f;
    }

    public static bool TryApply(HullVertex edgeStart, out IReadOnlyList<HullVertex> affected)
    {
        if (!TryExtendNeighborsToIntersection(edgeStart, out var intersection)) {
            affected = [];
            return false;
        }

        edgeStart.Point = intersection;
        var (_, newEdgeEnd) = edgeStart.Next.Unlink();
        affected = [edgeStart.Prev.Prev, edgeStart.Prev, edgeStart, newEdgeEnd];
        return true;
    }

    private static bool TryExtendNeighborsToIntersection(HullVertex edgeStart, out Vector2 intersection)
    {
        var start = edgeStart.Point;
        var end = edgeStart.Next.Point;
        var incomingDirection = start - edgeStart.Prev.Point;
        var outgoingDirection = edgeStart.Next.Next.Point - end;

        var denominator = Cross(incomingDirection, outgoingDirection);
        if (denominator == 0f) {
            intersection = default;
            return false;
        }

        var edge = end - start;
        var alongIncoming = Cross(edge, outgoingDirection) / denominator;
        var alongOutgoing = Cross(incomingDirection, edge) / denominator;

        intersection = start + alongIncoming * incomingDirection;
        return alongIncoming > 0f && alongOutgoing > 0f && float.IsFinite(intersection.X) && float.IsFinite(intersection.Y);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}
